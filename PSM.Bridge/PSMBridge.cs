using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByondSharp.FFI;
using JetBrains.Annotations;

namespace PSM.Bridge {
  [UsedImplicitly]
  public class PSMBridge {
    private const string BridgeVersion = "1.0.0";

    public static string PSMResponse(int status, string message = null) {
      return$@"{{""Status"":{status},""Message"":""{(message is null ? "null" : message.Replace("\"", "\\\""))}""}}";
    }

    public static string PSMOk(string   message = null) => PSMResponse(200, message);
    public static string PSMFail(string message = null) => PSMResponse(400, message);

    [ByondFFI]
    public static string GetBridgeVersion() {
      return PSMOk(BridgeVersion);
    }

    [ByondFFI]
    public static string SendBridgeTopic(List<string> args) {
      var agg = args.Aggregate((a, e) => $"{a}|{e}");
      if(args.Count != 4) return PSMFail($"Invalid argument count: {args.Count} != 4 / {agg}");

      var topic = args[0];
      var id    = int.Parse(args[1]);
      var port  = int.Parse(args[2]);
      var data  = args[3];

      var bridgeClient = new TcpClient();
      try {
        bridgeClient.Connect("localhost", port);
        Task.Run(() => {
                   while(!bridgeClient.Connected) Thread.Sleep(0);
                 }).Wait(500);
        var bridgeStream = bridgeClient.GetStream();

        bridgeStream.WriteTimeout = bridgeStream.ReadTimeout = 500;

        var packet = new List<byte>();

        var bytesId = BitConverter.GetBytes(id);
        packet.AddRange(BitConverter.GetBytes(bytesId.Length));
        packet.AddRange(bytesId);

        var bytesTopic = Encoding.ASCII.GetBytes(topic);
        packet.AddRange(BitConverter.GetBytes(bytesTopic.Length));
        packet.AddRange(bytesTopic);
        
        var bytesData = Encoding.ASCII.GetBytes(data);
        packet.AddRange(BitConverter.GetBytes(bytesData.Length));
        packet.AddRange(bytesData);

        bridgeStream.Write(packet.ToArray());
        bridgeStream.Flush();

        var response = new byte[sizeof(int)];
        if(bridgeStream.Read(response) != response.Length) throw new IOException("response size");
        response = new byte[BitConverter.ToInt32(response)];
        if(bridgeStream.Read(response) != response.Length) throw new IOException("response actual");

        bridgeClient.Close();
        return PSMOk(Encoding.ASCII.GetString(response));
      } catch {
        bridgeClient.Close();
        throw;
      }
    }
  }
}
