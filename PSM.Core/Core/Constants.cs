using System.Security.Cryptography;

namespace PSM.Core.Core {
  public static class Constants {
    public static class System {
      public const string DefaultInstanceDir  = "C:/PSM/instances/";
      public const string SystemUsername      = "SYSTEM";
      public const string SystemAdminUsername = "ADMIN";
      public const string SystemAdminPassword = "ChangeMeYouMuppet";
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

    public static List<PSMPermissions> AllPermissions =>
      new() {
              PSMPermissions.UserCreate,
              PSMPermissions.UserDelete,
              PSMPermissions.UserModify,
              PSMPermissions.UserEnable
            };

  }

  public enum PSMPermissions : ulong {
    // User permissions
    UserCreate = 1,
    UserDelete = 2,
    UserModify = 3,
    UserEnable = 4
  }
}
