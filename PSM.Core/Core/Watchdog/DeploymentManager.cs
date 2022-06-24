using System.Diagnostics;
using PSM.Core.Database.Tables;

namespace PSM.Core.Watchdog;

public class DeploymentManager {
  private readonly Instance         _instanceInformation;
  private readonly DirectoryInfo    _instanceRoot;
  private readonly InstanceWatchdog _instanceWatchdog;

  public DeploymentManager(InstanceWatchdog instanceWatchdog) {
    _instanceWatchdog    = instanceWatchdog;
    _instanceInformation = instanceWatchdog.InstanceActual;
    _instanceRoot        = _instanceInformation.GetInstanceRoot();
  }

  public DirectoryInfo? GetDeployment(Guid id, bool creating = false) {
    var deploy = new DirectoryInfo(Path.Join(_instanceRoot.InstanceDeployments().FullName, id.ToString()));
    if(creating && !deploy.Exists) {
      deploy.Create();
      return deploy;
    }

    return deploy.Exists ? deploy : null;
  }


  public Guid StartDeployment() {
    var id = Guid.NewGuid();
    while(GetDeployment(id) is { })
      id = Guid.NewGuid();
    var deployment = GetDeployment(id, true)!;
    Util.CopyDirectory(_instanceRoot.InstanceRepository(), deployment);
    PSMDirectory.BYOND_DownloadVersion(_instanceInformation.DreamMakerVersion);
    var dmExe = PSMDirectory.GetBYONDFile(_instanceInformation.DreamMakerVersion, "dm.exe");

    var dmPs = new ProcessStartInfo(dmExe.FullName,
                                    ArgHelper.DmArgs(_instanceInformation)) {
                                                                              UseShellExecute        = false,
                                                                              WorkingDirectory       = deployment.FullName,
                                                                              RedirectStandardOutput = true,
                                                                            };

    var dmP = Process.Start(dmPs);
    if(dmP is null)
      throw new ApplicationException("failed to launch DreamMaker");
    dmP.WaitForExit(_instanceInformation.DreamDaemonDeployTimeoutLength * 1000);
    if(!dmP.HasExited) dmP.Kill(true);

    var deployFailed = dmP.ExitCode != 0;
    if(deployFailed) {
      Console.WriteLine("!Deploy Failed!");
    } else {
      // create key file and link bridge dlls
      Util.CopyDirectory(new DirectoryInfo(PSMDirectory.BridgeDllInstall.FullName), new DirectoryInfo(Path.Join(deployment.FullName, "psm_bridge_api")), true);

      using var keyfile = File.CreateText(Path.Join(deployment.FullName, "psm.key"));
      keyfile.Write(_instanceInformation.DreamDaemonPSMKey);
      keyfile.Flush();
      keyfile.Dispose();
      if(_instanceInformation.DreamMakerApiValidation) {
        var validationPort = Random.Shared.Next(500, 600);
        var fakeInstance = new Instance {
                                          DreamDaemonDmeName    = _instanceInformation.DreamDaemonDmeName,
                                          DreamDaemonParams     = _instanceInformation.DreamDaemonParams,
                                          DreamDaemonPort       = validationPort,
                                          DreamDaemonTrustLevel = TrustLevel.Trusted,
                                          DreamDaemonVisibility = Visibility.Invisible
                                        };
        var args = ArgHelper.DdArgs(fakeInstance, validationPort);
        var ddPs = new ProcessStartInfo(
                                        PSMDirectory.GetBYONDFile(_instanceInformation.DreamMakerVersion,
                                                                  "dreamdaemon.exe").FullName, args) {
                                                                                                       WorkingDirectory = deployment.FullName
                                                                                                     };
        var ddId = Process.Start(ddPs);

        var validated = false;
        var attempts  = 0;
        while(!validated && attempts++ < 5) {
          Thread.Sleep(2000);
          var resp = _instanceWatchdog.SendPSMTopic("api_verify", portOverride: validationPort).GetAwaiter().GetResult();
          Console.WriteLine($"Validation Response: {resp ?? "null"}");
          if(resp == "psm_ok")
            validated = true;
        }

        if(!validated) {
          Console.WriteLine("Failed to validate API");
          deployFailed = true;
        }

        ddId.Kill(true);
      }
    }

    if(deployFailed)
      deployment.Delete(true);
    return id;
  }

  public void DeleteDeployment(Guid id) {
    if(_instanceInformation.DreamDaemonDeployActive.Equals(id))
      throw new InvalidOperationException("Cannot delete the active deployment");
    GetDeployment(id)?.Delete();
  }

  public void SetActiveDeployment(Guid deployment) {
    if(GetDeployment(deployment) is not { } deployDir)
      throw new ArgumentOutOfRangeException(nameof(deployDir), $"deployment '{deployment}' does not exist");
    var instanceLive = _instanceRoot.InstanceLive().FullName;
    if(Directory.Exists(instanceLive))
      Directory.Delete(instanceLive, true);
    Directory.CreateSymbolicLink(instanceLive, deployDir.FullName);
    _instanceInformation.DreamDaemonDeployActive = deployment;
  }
}
