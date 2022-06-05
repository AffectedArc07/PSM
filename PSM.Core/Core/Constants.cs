﻿using System.Security.Cryptography;
using PSM.Core.Core.Database.Tables;
using PSM.Core.Core.Database.Tables.Abstract;
using PSM.Core.Models.API;

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

      public const int SystemUserID = 1;
      public const int AdminUserID  = 2;
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

    public static string AdminPermissionString { get; } = PermissionSet.PermissionListToString(Enum.GetValues<PSMPermission>());

    public static ILogger AppLog { get; set; } = null!;

    public static List<PSMPermission> ConvertToPermissionList(this string permString) => permString.Trim(';').Split(';', StringSplitOptions.RemoveEmptyEntries).Select(v => (PSMPermission)int.Parse(v)).DistinctBy(p => (int)p).ToList();

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

    public static UserInformationModel GetInformationModel(this User user) {
      return new UserInformationModel {
                                        Enabled  = user.Enabled,
                                        UserID   = user.Id,
                                        Username = user.Username,
                                        Archived = user.Archived
                                      };
    }

    public static PermissionInformationModel GetInformationModel(this PSMPermission permission) {
      return permission switch {
               PSMPermission.UserCreate => new PermissionInformationModel {
                                                                            Id          = (int)PSMPermission.UserCreate,
                                                                            Name        = "UserCreate",
                                                                            Description = "Create a new User",
                                                                          },
               PSMPermission.UserModify => new PermissionInformationModel {
                                                                            Id          = (int)PSMPermission.UserModify,
                                                                            Name        = "UserModify",
                                                                            Description = "Modify an existing User",
                                                                          },
               PSMPermission.UserEnable => new PermissionInformationModel {
                                                                            Id          = (int)PSMPermission.UserEnable,
                                                                            Name        = "UserEnable",
                                                                            Description = "Enable or Disable an existing User",
                                                                          },
               PSMPermission.UserList => new PermissionInformationModel {
                                                                          Id          = (int)PSMPermission.UserList,
                                                                          Name        = "UserList",
                                                                          Description = "List all Users",
                                                                        },
               PSMPermission.UserRename => new PermissionInformationModel {
                                                                            Id          = (int)PSMPermission.UserRename,
                                                                            Name        = "UserRename",
                                                                            Description = "Change the username of a User"
                                                                          },
               PSMPermission.UserArchive => new PermissionInformationModel {
                                                                             Id          = (int)PSMPermission.UserArchive,
                                                                             Name        = "UserArchive",
                                                                             Description = "Archive a User"
                                                                           },
               _ => throw new ArgumentException(null, nameof(permission))
             };
    }
  }

  public enum PSMPermission {
    // User permissions
    UserCreate  = 1,
    UserModify  = 2,
    UserEnable  = 3,
    UserList    = 4,
    UserRename  = 5,
    UserArchive = 6,
  }
}
