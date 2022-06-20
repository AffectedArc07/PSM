namespace PSM.Core.Watchdog;

public class WaitSource : IDisposable {
  public WaitSource(WatchdogToken holder, CancellationTokenSource source) {
    _holder = holder;
    _source = source;
  }

  private readonly WatchdogToken           _holder;
  private readonly CancellationTokenSource _source;
  private          bool                    _finished;

  public void Finish() {
    if(_finished) return;
    _finished = true;
    _source.Cancel();
    _holder.Wait();
    _source.Dispose();
  }

  void IDisposable.Dispose() {
    GC.SuppressFinalize(this);
    Finish();
  }

  ~WaitSource() { // You really cannot trust using statements to call Dispose, ffs
    Finish();
  }
}

public class WatchdogToken {
  private bool _work;

  public WaitSource StartWork() {
    if(_work) Wait();
    _work = true;

    var token = new CancellationTokenSource();
    token.Token.Register(EndWork);
    return new WaitSource(this, token);
  }

  private void EndWork() {
    _work = false;
  }

  public void Wait() {
    while(_work) Thread.Sleep(0);
  }
}
