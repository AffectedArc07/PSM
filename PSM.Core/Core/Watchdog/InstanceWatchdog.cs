using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NuGet.ProjectModel;
using PSM.Core.Database;
using PSM.Core.Database.Tables;

namespace PSM.Core.Watchdog;

public class InstanceWatchdog {
  protected static readonly Dictionary<int, InstanceWatchdog> Watchdogs = new();
  private static            TcpListener?                      _loopbackListener;

  public static async Task StartLoopback() {
    async Task HandleClient(TcpClient client) {
      await Task.Run(() => {
                       try {
                         Console.WriteLine("Handling Client");
                         var ddStream = client.GetStream();

                         ddStream.ReadTimeout = ddStream.WriteTimeout = 500;

                         var id = new byte[sizeof(int)];
                         if(ddStream.Read(id) != id.Length) throw new IOException();
                         id = new byte[BitConverter.ToInt32(id)];
                         if(ddStream.Read(id) != id.Length) throw new IOException();
                         var iID = BitConverter.ToInt32(id);
                         Console.WriteLine($"Read ID: {iID}");

                         var topic = new byte[sizeof(int)];
                         if(ddStream.Read(topic) != topic.Length) throw new IOException();
                         topic = new byte[BitConverter.ToInt32(topic)];
                         if(ddStream.Read(topic) != topic.Length) throw new IOException();
                         var iTopic = Encoding.ASCII.GetString(topic);
                         Console.WriteLine($"Read Topic: {iTopic}");

                         var data = new byte[sizeof(int)];
                         if(ddStream.Read(data) != data.Length) throw new IOException();
                         data = new byte[BitConverter.ToInt32(data)];
                         if(ddStream.Read(data) != data.Length) throw new IOException();
                         var iData = JsonSerializer.Deserialize<List<string>>(Encoding.ASCII.GetString(data))!;
                         Console.WriteLine(iData.Count > 0 ? $"Read Data: {iData.Aggregate((a, s) => $"{a};{s}")}" : "no data");

                         var iResponse = !Watchdogs.TryGetValue(iID, out var instanceWatchdog)
                                           ? "psm_topic_not_found"
                                           : instanceWatchdog.HandleBridgeTopic(iTopic, iData);

                         var response = Encoding.ASCII.GetBytes(iResponse);
                         ddStream.Write(BitConverter.GetBytes(response.Length));
                         ddStream.Write(response);
                         ddStream.Flush();
                       } catch(Exception e) {
                         Console.WriteLine($"Failed to handle client: {e}");
                       }
                     });
    }

    Console.WriteLine($"Starting loopback on {Constants.WatchdogLoopbackPort}");
    if(_loopbackListener is { }) return;

    try {
      _loopbackListener = new TcpListener(IPAddress.Any, Constants.WatchdogLoopbackPort) {
                                                                                           ExclusiveAddressUse = true
                                                                                         };
      _loopbackListener.Start();
      Console.WriteLine("Created Loopback listener");
    } catch(Exception e) {
      Console.WriteLine("Failed to create loopback listener!");
      Console.WriteLine(e.ToString());
      _loopbackListener = null;
      return;
    }

    while(Watchdogs.Any(kvp => !kvp.Value.InstanceCancellationToken.IsCancellationRequested)) {
      var ddClient = await _loopbackListener.AcceptTcpClientAsync();
      await HandleClient(ddClient);
      ddClient.Close();
    }

    _loopbackListener.Stop();
    _loopbackListener = null;
  }

  protected InstanceContext InstanceContext;
  protected Instance        InstanceActual;

  protected CancellationToken InstanceCancellationToken;

  // DO NOT FORGET TO USE THE WATCHDOG TOKEN. PLEASE -ZephyrTFA
  protected readonly WatchdogToken WatchdogToken = new();

  // DO NOT TOUCH THIS OUTSIDE ATTACH AND DETACH
  protected CancellationTokenSource WatchdogAttachSource { get; private set; } = new();

  protected int      DdProcessID = -1;
  protected DateTime DdProcessStart;
  protected bool     InstanceStarted;

  public InstanceWatchdog(Instance target, InstanceContext holder) {
    if(Watchdogs.TryGetValue(target.Id, out var existing)) {
      existing.Detach();
      DdProcessID    = existing.DdProcessID;
      DdProcessStart = existing.DdProcessStart;
    }

    InstanceContext = holder;
    InstanceActual  = target;
    _getPersistenceDD();
    Watchdogs[target.Id] = this;
  }

  public void Detach() {
    using var tok = WatchdogToken.StartWork();
    var wdData = new Dictionary<string, string> {
                                                  ["wd_id"] = $"{InstanceActual.Id}"
                                                };
    SendPSMTopic("watchdog_attach", _dictToData(wdData)).GetAwaiter().GetResult();
    if(_hbReattaching)
      return;

    WatchdogAttachSource.Cancel();
    _heartbeatTask.Wait(default(CancellationToken));
  }

  public bool Attach() {
    using var tok = WatchdogToken.StartWork();
    if(!InstanceStarted)
      return false;
    if(!_hbReattaching) {
      if(!WatchdogAttachSource.TryReset())
        WatchdogAttachSource = new CancellationTokenSource();
      InstanceCancellationToken = WatchdogAttachSource.Token;
      _heartbeatTask            = HeartbeatLoop();
    }
#pragma warning disable CS4014
    StartLoopback();
    Thread.Sleep(1000); // give it a second to initialize loopback
#pragma warning restore CS4014

    var wdData = new Dictionary<string, string> {
                                                  ["wd_id"]   = $"{InstanceActual.Id}",
                                                  ["wd_port"] = $"{Constants.WatchdogLoopbackPort}"
                                                };
    var attachData = _dictToData(wdData);
    var attachResp = SendPSMTopic("watchdog_attach", attachData).GetAwaiter().GetResult();
    if(attachResp != "psm_ok") Console.WriteLine("Failed to register loopback port on Instance-{0}", InstanceActual.Id);
    return true;
  }

  private Task? _heartbeatTask;
  private bool  _hbReattaching;

  private async Task HeartbeatLoop() {
    var hbFails = 0;
    Console.WriteLine($"Launching HBLoop with interval of {InstanceActual.DreamDaemonHeartbeatInterval}");
    while(InstanceStarted) {
      await Task.Delay(TimeSpan.FromSeconds(InstanceActual.DreamDaemonHeartbeatInterval), InstanceCancellationToken);
      if(!InstanceStarted)
        break;

      Console.WriteLine("Sending Heartbeat");
      var resp = await SendPSMTopic("watchdog_ping");
      if(resp == "psm_ok") {
        hbFails = 0;
        continue;
      }

      hbFails++;
      Console.WriteLine($"heartbeat failure #{hbFails}");
      switch(hbFails) {
        case> 5:
          // kill and restart
          break;
        case> 4:
          _hbReattaching = true;
          Detach();
          Attach();
          _hbReattaching = true;
          break;
      }
    }
  }

  private static string _dictToData(Dictionary<string, string> dict) {
    var json = "{";
    foreach(var (key, value) in dict)
      json += @$"""{key}"":{(value.StartsWith("{") ? "" : "\"")}{value}{(value.StartsWith("{") ? "" : "\"")},";
    if(json.EndsWith(","))
      json = json[..^1];
    return json + "}";
  }

  public static void Main() {
    Constants.ConstantInit();
    if(!Constants.ProcessRunsAsAdmin) {
      Console.WriteLine("Program must be ran as Admin!");
      Environment.Exit(-1);
      return;
    }

    var _ = new InstanceWatchdog(new Instance {
                                                Id                           = 1,
                                                DreamDaemonAddress           = "127.0.0.1",
                                                DreamDaemonPort              = 5565,
                                                DreamDaemonHeartbeatInterval = 2,
                                                RootPath                     = "C:/PSM/TestInstance/",
                                                DreamDaemonDmeName           = "testinstance.dme",
                                              }, null!);
    if(!_.DreamDaemon_Deploy().GetAwaiter().GetResult()) {
      Console.WriteLine("Failed to deploy");
      return;
    }

    _.DreamDaemon_Launch().Wait();
    Thread.Sleep(1000);
    if(!_.Attach())
      Console.WriteLine("Failed to attach to instance");
    Thread.Sleep(10000);
    _.DreamDaemon_Shutdown().Wait();
  }

  protected async Task<string?> SendPSMTopic(string topic, string data = "") {
    try {
      using var ddClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      await ddClient.ConnectAsync(new IPEndPoint(IPAddress.Parse(InstanceActual.DreamDaemonAddress), InstanceActual.DreamDaemonPort), InstanceCancellationToken);
      ddClient.ReceiveTimeout = ddClient.SendTimeout = 5000;

      var psmHeader = _dictToData(new Dictionary<string, string> {
                                                                   ["psm_topic"] = topic,
                                                                   ["psm_key"]   = InstanceActual.DreamDaemonPSMKey,
                                                                   ["psm_data"]  = data
                                                                 });
      var    query            = $"?{psmHeader}";
      byte[] byondTopicHeader = { 0, 0x83, 0, 0, 0, 0, 0, 0, 0 };
      var    queryBytes       = Encoding.ASCII.GetBytes(query);

      var idx    = 0;
      var packet = new byte[byondTopicHeader.Length + queryBytes.Length + 1];

      // assemble packet
      foreach(var tByte in byondTopicHeader)
        packet[idx++] = tByte;
      foreach(var qByte in queryBytes)
        packet[idx++] = qByte;
      packet[idx] = 0;

      // splice in length data
      var pLengthBytes = BitConverter.GetBytes((short)(packet.Length - 4));
      if(BitConverter.IsLittleEndian)
        pLengthBytes = pLengthBytes.Reverse().ToArray();
      packet[2] = pLengthBytes[0];
      packet[3] = pLengthBytes[1];

      var sent = await ddClient.SendAsync(new ReadOnlyMemory<byte>(packet), SocketFlags.None, InstanceCancellationToken);
      if(sent != packet.Length) throw new IOException("Failed to send data");

      var rcvBuff = new byte[512];
      var rcv     = await ddClient.ReceiveAsync(new Memory<byte>(rcvBuff), SocketFlags.None, InstanceCancellationToken);

      // IO completed, disconnect and release handles
      await ddClient.DisconnectAsync(false, InstanceCancellationToken);
      ddClient.Close();
      ddClient.Dispose();

      return rcv <= 5 ? string.Empty : Encoding.ASCII.GetString(rcvBuff[5..(rcv - 1)]);
    } catch(Exception) {
      return null;
    }
  }

  public Task DreamDaemon_Launch() {
    if(DdProcessID != -1) {
      InstanceStarted = true;
      return Task.CompletedTask; // already started
    }

    var root = new DirectoryInfo(InstanceActual.RootPath);
    if(!root.Exists)
      throw new DirectoryNotFoundException("root");

    var byondInstall = new DirectoryInfo(Path.Join(root.FullName, "BYOND"));
    if(!byondInstall.Exists)
      throw new DirectoryNotFoundException("ddInstall");

    var byondBin = new DirectoryInfo(Path.Join(byondInstall.FullName, "bin"));
    if(!byondInstall.Exists)
      throw new DirectoryNotFoundException("ddBin");

    var ddLive = new DirectoryInfo(Path.Join(root.FullName, "Live"));
    if(!ddLive.Exists)
      throw new DirectoryNotFoundException("ddLive");

    var ddDme = ddLive.GetFiles(InstanceActual.DreamDaemonDmeName).FirstOrDefault();
    if(ddDme is null || !ddDme.Exists)
      throw new FileNotFoundException("ddDme");
    if(ddDme.Extension != ".dme")
      throw new FileFormatException("ddDme");

    var ddExe = new FileInfo(Path.Join(byondBin.FullName, "dreamdaemon.exe"));
    if(!ddExe.Exists)
      throw new FileNotFoundException("ddExe");

    var ddSi = new ProcessStartInfo(ddExe.FullName, _dd_args()) {
                                                                  UseShellExecute  = true,
                                                                  WorkingDirectory = ddLive.FullName
                                                                };

    var ddP = Process.Start(ddSi);
    DdProcessID    = ddP.Id;
    DdProcessStart = ddP.StartTime;
    _savePersistenceDD();
    InstanceStarted = true;
    return Task.CompletedTask;
  }

  public async Task DreamDaemon_Shutdown() {
    using var token = WatchdogToken.StartWork();

    var ddProcess = Process.GetProcessById(DdProcessID);
    if(ddProcess.StartTime.Ticks != DdProcessStart.Ticks)
      throw new ApplicationException("failed to validate start time information");

    if(await SendPSMTopic("world_shutdown") == "psm_ok") {
      InstanceStarted = false;
      Thread.Sleep(5000); // give it time to perform shutdown tasks if we get the expected response
      ddProcess.Kill(true);
    } else {
      if(!ddProcess.WaitForExit(5000)) {
        // topic call failed
        Console.WriteLine("Failed to close DreamDaemon nicely.");
        ddProcess.Kill(false);
      }

      if(!ddProcess.WaitForExit(5000)) {
        // SIGTERM failed
        Console.WriteLine("Failed to close DreamDaemon aggressively");
        ddProcess.Kill(true);
      }

      if(!ddProcess.WaitForExit(2000)) {
        Console.WriteLine("Failed to force kill DreamDaemon");
        throw new ApplicationException("failed to kill DreamDaemon");
      }
    }

    InstanceStarted = false;
    DdProcessID     = -1;
    _savePersistenceDD();
  }

  public async Task<bool> DreamDaemon_Deploy() {
    var root = new DirectoryInfo(InstanceActual.RootPath);
    if(!root.Exists)
      throw new DirectoryNotFoundException("root");

    var byondInstall = new DirectoryInfo(Path.Join(root.FullName, "BYOND"));
    if(!byondInstall.Exists)
      throw new DirectoryNotFoundException("ddInstall");

    var byondBin = new DirectoryInfo(Path.Join(byondInstall.FullName, "bin"));
    if(!byondInstall.Exists)
      throw new DirectoryNotFoundException("ddBin");

    var ddLive = new DirectoryInfo(Path.Join(root.FullName, "Live"));
    if(!ddLive.Exists)
      throw new DirectoryNotFoundException("ddLive");

    var ddDme = ddLive.GetFiles(InstanceActual.DreamDaemonDmeName).FirstOrDefault();
    if(ddDme is null || !ddDme.Exists)
      throw new FileNotFoundException("ddDme");
    if(ddDme.Extension != ".dme")
      throw new FileFormatException("ddDme");

    var dmExe = new FileInfo(Path.Join(byondBin.FullName, "dm.exe"));
    if(!dmExe.Exists)
      throw new FileNotFoundException("ddExe");

    var dmPs = new ProcessStartInfo(dmExe.FullName, _dm_args()) {
                                                                  UseShellExecute        = false,
                                                                  WorkingDirectory       = ddLive.FullName,
                                                                  RedirectStandardOutput = true
                                                                };
    var dmP = Process.Start(dmPs);
    if(dmP is null)
      throw new ApplicationException("failed to launch DreamMaker");

    dmP.WaitForExit(InstanceActual.DreamDaemonDeployTimeoutLength * 1000);
    Console.WriteLine("Deploy Output:");
    Console.WriteLine(await dmP.StandardOutput.ReadToEndAsync());
    Console.WriteLine($"Exit Code: {dmP.ExitCode}");

    var keyfile = new FileInfo(Path.Join(ddLive.FullName, "psm.key"));
    if(keyfile.Exists)
      keyfile.Delete();
    await using var writer = keyfile.CreateText();
    await writer.WriteAsync(InstanceActual.DreamDaemonPSMKey);
    await writer.FlushAsync();
    await writer.DisposeAsync();

    var bridgeDllLnk = new DirectoryInfo(Path.Join(ddLive.FullName,            "psm_bridge_api"));
    var bridgeDllDir = new DirectoryInfo(Path.Join(Constants.PSMRootDirectory, "Install", "psm_bridge_api"));

    if(bridgeDllLnk.Exists && bridgeDllLnk.Attributes.HasFlag(FileAttributes.ReparsePoint))
      bridgeDllLnk.Delete();
    if(!bridgeDllDir.Exists)
      throw new FileNotFoundException(nameof(bridgeDllDir));
    Directory.CreateSymbolicLink(bridgeDllLnk.FullName, bridgeDllDir.FullName);

    return dmP.ExitCode == 0;
  }

  private string _dd_args() {
    return@$"{InstanceActual.DreamDaemonDmeName[..^4]}.dmb {InstanceActual.DreamDaemonPort} -trusted";
  }

  private string _dm_args() {
    return$"-clean {InstanceActual.DreamDaemonDmeName}";
  }

  private void _getPersistenceDD() {
    var root = new DirectoryInfo(InstanceActual.RootPath);
    if(!root.Exists)
      throw new DirectoryNotFoundException("root");

    var pD = new FileInfo(Path.Join(root.FullName, "persistence_dd.lock"));
    if(!pD.Exists)
      return;

    var       buffPid = new byte[sizeof(int)];
    var       buffDto = new byte[sizeof(long)];
    using var reader  = pD.OpenRead();

    if(reader.Read(buffPid) != buffPid.Length)
      throw new IOException();
    if(reader.Read(buffDto) != buffDto.Length)
      throw new IOException();

    DdProcessID    = BitConverter.ToInt32(buffPid);
    DdProcessStart = new DateTime(BitConverter.ToInt64(buffDto));

    try {
      Process.GetProcessById(DdProcessID).Close();
    } catch(ArgumentException) {
      DdProcessID = -1;
    }
  }

  private void _savePersistenceDD() {
    var root = new DirectoryInfo(InstanceActual.RootPath);
    if(!root.Exists)
      throw new DirectoryNotFoundException("root");

    var pD = new FileInfo(Path.Join(root.FullName, "persistence_dd.lock"));
    if(pD.Exists)
      pD.Delete();

    var buffPid = BitConverter.GetBytes(DdProcessID);
    var buffDto = BitConverter.GetBytes(DdProcessStart.Ticks);

    using var stream = pD.Create();
    stream.SetLength(buffPid.Length + buffDto.Length);
    stream.Write(buffPid);
    stream.Write(buffDto);
    stream.Flush();
    stream.Dispose();
  }

  private string HandleBridgeTopic(string topic, List<string> data) {
    Console.WriteLine($"Bridge Topic: {topic}");
    switch(topic) {
      case"world_shutdown":
        Console.WriteLine("World Stopped");
        break;

      case"world_reboot":
        Console.WriteLine("World Rebooted");
        break;

      default:
        Console.WriteLine("Unknown Topic");
        return"psm_topic_not_found";
    }

    return"psm_topic_not_handled";
  }
}
