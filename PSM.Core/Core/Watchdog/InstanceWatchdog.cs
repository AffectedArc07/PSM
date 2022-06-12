using PSM.Core.Database;

namespace PSM.Core.Watchdog;

public class InstanceWatchdog {
  protected static readonly Dictionary<int, InstanceWatchdog> Watchdogs = new();
  protected                 InstanceContext                   InstanceContext;

  public InstanceWatchdog(Database.Tables.Instance target, InstanceContext holder) {
    if(Watchdogs.TryGetValue(target.Id, out var existing)) {
      if(!existing.Detach())
        throw new ApplicationException("Failed to detach existing Watchdog");
    }

    InstanceContext      = holder;
    Watchdogs[target.Id] = this;
    Attach();
  }

  private bool _workActive = false;

  public void WaitForIdle() {
    while(_workActive) Thread.Sleep(0);
  }

  public bool Detach() {
    return false;
  }

  public bool Attach() {
    return false;
  }
}
