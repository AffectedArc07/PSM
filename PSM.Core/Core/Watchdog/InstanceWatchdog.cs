using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NuGet.ProjectModel;
using PSM.Core.Database;
using PSM.Core.Database.Tables;

namespace PSM.Core.Watchdog;

public class InstanceWatchdog {
  protected static readonly Dictionary<int, InstanceWatchdog> Watchdogs = new();
  private static            Socket?                           _loopbackListener;

  public static async Task StartLoopback() {
    Console.WriteLine("Starting loopback");
    if(_loopbackListener is { }) return;

    _loopbackListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    _loopbackListener.Bind(new IPEndPoint(IPAddress.Loopback, Constants.WatchdogLoopbackPort));
    _loopbackListener.Listen();

    while(Watchdogs.All(kvp => kvp.Value.InstanceCancellationToken.IsCancellationRequested)) {
      await Task.Delay(25);

      try {
        var ddClient = await _loopbackListener.AcceptAsync();
        var ddStream = new NetworkStream(ddClient, true);

        // todo
        // I CANNOT GET THIS SHIT TO FUCKING WORK

        ddStream.Close();
      } catch(Exception e) {
        Console.WriteLine(e);
      }
    }

    _loopbackListener.Close();
    _loopbackListener.Dispose();
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
    SendPSMTopic("psm_watchdog_detach", _dictToData(wdData)).GetAwaiter().GetResult();
    if(_hbReattaching)
      return;

    WatchdogAttachSource.Cancel();
    _heartbeatTask.Wait(default(CancellationToken));
  }

  public bool Attach() {
    using var tok = WatchdogToken.StartWork();
    if(!_hbReattaching) {
      if(!WatchdogAttachSource.TryReset())
        WatchdogAttachSource = new CancellationTokenSource();
      InstanceCancellationToken = WatchdogAttachSource.Token;
      _heartbeatTask            = HeartbeatLoop();
    }
#pragma warning disable CS4014
    StartLoopback();
#pragma warning restore CS4014

    var wdData = new Dictionary<string, string> {
                                                  ["wd_id"]   = $"{InstanceActual.Id}",
                                                  ["wd_port"] = $"{Constants.WatchdogLoopbackPort}"
                                                };
    var attachData = _dictToData(wdData);
    var attachResp = SendPSMTopic("psm_watchdog_attach", attachData).GetAwaiter().GetResult();
    if(attachResp != "psm_okay") Console.WriteLine("Failed to register loopback port on Instance-{0}", InstanceActual.Id);
    Console.WriteLine($"attach: {attachResp}");
    return true;
  }

  private Task? _heartbeatTask;
  private bool  _hbReattaching;

  private async Task HeartbeatLoop() {
    var hbFails = 0;
    Console.WriteLine($"Launching HBLoop with interval of {InstanceActual.DreamDaemonHeartbeatInterval}");
    while(!InstanceCancellationToken.IsCancellationRequested) {
      await Task.Delay(TimeSpan.FromSeconds(InstanceActual.DreamDaemonHeartbeatInterval), InstanceCancellationToken);
      Console.WriteLine("Sending Heartbeat");
      var heartbeatData = new Dictionary<string, string> { ["wd_id"] = $"{InstanceActual.Id}" };
      var resp          = await SendPSMTopic("psm_watchdog_heartbeat", _dictToData(heartbeatData));
      if(resp == "psm_okay") {
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
    var _ = new InstanceWatchdog(new Instance {
                                                Id                           = 1,
                                                DreamDaemonAddress           = "127.0.0.1",
                                                DreamDaemonPort              = 5565,
                                                DreamDaemonHeartbeatInterval = 1,
                                                RootPath                     = "C:/PSM/TestInstance/",
                                                DreamDaemonDmeName           = "testinstance.dme"
                                              }, null!);
    if(!_.DreamDaemon_Deploy().GetAwaiter().GetResult()) {
      Console.WriteLine("Failed to deploy");
      return;
    }

    _.DreamDaemon_Launch().Wait();
    Thread.Sleep(5000);
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
    } catch(Exception e) {
      Console.WriteLine($"Topic Send failure: {e}");
      return string.Empty;
    }
  }

  public async Task DreamDaemon_Launch() {
    if(DdProcessID != -1)
      return; // already started

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
  }

  public async Task DreamDaemon_Shutdown() {
    using var token = WatchdogToken.StartWork();

    var ddProcess = Process.GetProcessById(DdProcessID);
    if(ddProcess.StartTime.Ticks != DdProcessStart.Ticks)
      throw new ApplicationException("failed to validate start time information");

    if(await SendPSMTopic("psm_dd_shutdown") == "psm_world_del") {
      Thread.Sleep(5000); // give it time to perform shutdown tasks if we get the expected response
      ddProcess.Kill(true);
    } else {
      if(!ddProcess.WaitForExit(5000)) {
        // topic call failed
        Console.WriteLine("Failed to close DreamDaemon nicely.");
        ddProcess.Kill(false);
      }

      if(!ddProcess.WaitForExit(5000)) {
        // SIGTERM failed, send SIGHALT
        Console.WriteLine("Failed to close DreamDaemon aggressively");
        ddProcess.Kill(true);
      }

      if(!ddProcess.WaitForExit(2000)) {
        Console.WriteLine("Failed to force kill DreamDaemon");
        throw new ApplicationException("failed to kill DreamDaemon");
      }
    }

    DdProcessID = -1;
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
}
