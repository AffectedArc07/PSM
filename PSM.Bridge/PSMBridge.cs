using ByondSharp.FFI;
using JetBrains.Annotations;

namespace PSM.Bridge {
  [UsedImplicitly]
  public class PSMBridge {
    private const string BridgeVersion = "1.0.0";
    
    public static string PSMResponse(int status, string message = null) {
      return$@"{{""Status"":{status},""Message"":""{(message is null ? "null" : message.Replace("\"", "\\\""))}""}}";
    }

    public static string PSMOk(string message = null) => PSMResponse(200, message);
    
    [ByondFFI]
    public static string GetBridgeVersion() {
      return PSMOk(BridgeVersion);
    }
  }
}
