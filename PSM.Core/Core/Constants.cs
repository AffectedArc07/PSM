using System.Security.Cryptography;

namespace PSM.Core.Core {
  public static class Constants {
    public static class Config {
      //todo: make this not a fucking static class in the constants folder
      public static bool ReverseProxyEnabled = true;
    }

    public static class System {
      public const string DefaultInstanceDir  = "C:/PSM/instances/";
      public const string SystemUsername      = "SYSTEM";
      public const string SystemAdminUsername = "ADMIN";
      public const string SystemAdminPassword = "ChangeMeYouMuppet";

      public const int UsernameMinimumLength = 8;
      public const int UsernameMaximumLength = 64;
      public const int PasswordMinimumLength = 8;
      public const int PasswordMaximumLength = 64;
    }

    public static class ExitCodes {
      public const int SystemUserEnabled = 125;
    }

    public static class JWT {
      public const int ByteMapLength = 256;


      public static byte[] GetByteMap() {
        _byteMap ??= RandomNumberGenerator.GetBytes(ByteMapLength);
        return(byte[])_byteMap.Clone(); // Pass them a copy of the original array or they can modify it and mess EVERYTHING up
      }

      private static byte[]? _byteMap;
    }

    public static string  AllPermissions => Enum.GetValues<PSMPermission>().ToList().ConvertToPermissionString();
    public static ILogger AppLog         { get; set; } = null!;

    public static string              ConvertToPermissionString(this IEnumerable<PSMPermission> permList)   => permList.Aggregate("", (current, psmPermission) => $"{current};{(ulong)psmPermission}").Trim(';');
    public static List<PSMPermission> ConvertToPermissionList(this   string                     permString) => permString.Split(";").Select(Enum.Parse<PSMPermission>).ToList();

    public static string GetRemoteFromContext(HttpContext context) {
      if(!Config.ReverseProxyEnabled)
        return context.Connection.RemoteIpAddress.ToString();

      var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString().Trim();
      var comma        = forwardedFor.IndexOf(',');
      if(comma != -1)
        forwardedFor = forwardedFor[..comma];
      if(string.IsNullOrWhiteSpace(forwardedFor))
        return context.Connection.RemoteIpAddress.ToString();
      return forwardedFor;
    }
  }


  public enum PSMPermission : ulong {
    // User permissions
    UserCreate = 1,
    UserDelete = 2,
    UserModify = 3,
    UserEnable = 4
  }

  public enum PSMResponse {
    Ok, NotFound, NoPermission
  }
}
