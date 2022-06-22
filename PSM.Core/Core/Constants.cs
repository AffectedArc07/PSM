using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.OpenApi.Extensions;
using Namotion.Reflection;
using PSM.Core.Database.Tables;
using PSM.Core.Database.Tables.Abstract;
using PSM.Core.Models.API;

namespace PSM.Core {
  public static class Constants {
    private static bool constantsInit = false;

    public static void ConstantInit() {
      if(constantsInit) throw new InvalidOperationException();
      constantsInit = true;
      if(Environment.OSVersion.Platform == PlatformID.Win32NT) WindowsInit();
      else OtherInit();
    }

    private static void WindowsInit() {
#pragma warning disable CA1416
      ProcessRunsAsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416
    }

    private static void OtherInit() {
      throw new NotSupportedException();
    }

    public static bool ProcessRunsAsAdmin;

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

    public static int WatchdogLoopbackPort = 5515;

    public static UserInformationModel GetInformationModel(this User user) {
      return new UserInformationModel {
                                        Enabled  = user.Enabled,
                                        UserID   = user.Id,
                                        Username = user.Username,
                                        Archived = user.Archived
                                      };
    }

    public static PermissionInformationModel GetInformationModel(this PSMPermission permission) {
      var pType   = permission.GetType();
      var summary = pType.GetXmlDocsSummary();
      return new PermissionInformationModel {
                                              Name        = permission.GetDisplayName(),
                                              Description = summary,
                                              Id          = (int)permission,
                                            };
    }

    public static InstanceInformationModel GetInformationModel(this Database.Tables.Instance instance) =>
      new() {
              ID   = instance.Id,
              Name = instance.Name
            };

    public static byte[] GetUnicodeBytes(this string str) {
      return Encoding.Unicode.GetBytes(str);
    }

    public static string GetUnicodeString(this byte[] bytes) {
      return Encoding.Unicode.GetString(bytes.AsSpan());
    }

    public static string PSMRootDirectory = "C:\\PSM\\";
  }

  public enum PSMPermission {
    // -- Global Permissions -- \\
    /// <summary>
    /// Create a user
    /// </summary>
    UserCreate = 1,

    /// <summary>
    /// Change the username of a user
    /// </summary>
    UserRename = 2,

    /// <summary>
    /// Modify the permissions of a user
    /// </summary>
    UserEdit = 3,

    /// <summary>
    /// Enable or disable a user
    /// </summary>
    UserEnable = 33,

    /// <summary>
    /// Archive or De-Archive a user
    /// </summary>
    UserArchive = 4,

    // Instance
    /// <summary>
    /// Create a new instance
    /// </summary>
    InstanceCreate = 5,

    /// <summary>
    /// View the event log of any instance
    /// </summary>
    InstanceInvestigate = 6,

    /// <summary>
    /// View all instances regardless of permissions
    /// </summary>
    InstanceAdminView = 7,

    /// <summary>
    /// Grant self all permissions on any instance
    /// </summary>
    InstanceGrantAdminSelf = 8,

    /// <summary>
    /// Grant a specific user all permissions on any instance
    /// </summary>
    InstanceGrantAdminOther = 9,

    // -- Instance Permissions -- \\
    /// <summary>
    /// Read the permission sets on an instance
    /// </summary>
    InstancePermissionRead = 10,

    /// <summary>
    /// Write and modify existing permission sets
    /// </summary>
    InstancePermissionWrite = 11,

    /// <summary>
    /// Read the event log of the repository and git actions
    /// </summary>
    InstanceRepoInvestigate = 12,

    /// <summary>
    /// Checkout a specific sha, branch, or tag
    /// </summary>
    InstanceRepoCheckoutSpecific = 13,

    /// <summary>
    /// Perform a hard reset on the local git state
    /// </summary>
    InstanceRepoHardReset = 14,

    /// <summary>
    /// Change the default branch of the repository
    /// </summary>
    InstanceRepoDefaultBranch = 15,

    /// <summary>
    /// Delete the local git state and repository
    /// </summary>
    InstanceRepoDelete = 16,

    /// <summary>
    /// Manage test merges on the repository
    /// </summary>
    InstanceRepoTestMerge = 17,

    /// <summary>
    /// I dont remember what this does, help
    /// </summary>
    InstanceRepoAdmin = 18,

    /// <summary>
    /// Overwrite git and repository credentials
    /// Credentials are never readable by anyone
    /// </summary>
    InstanceRepoCredentials = 19,

    /// <summary>
    /// Change auto update settings
    /// </summary>
    InstanceRepoAutoUpdate = 20,

    /// <summary>
    /// Change automatic updates on submodules
    /// </summary>
    InstanceRepoSubmodule = 21,

    // Byond
    /// <summary>
    /// Read the current BYOND information
    /// </summary>
    InstanceByondRead = 22,

    /// <summary>
    /// Read the BYOND event log
    /// </summary>
    InstanceByondInvestigate = 23,

    /// <summary>
    /// Install new BYOND versions
    /// </summary>
    InstanceByondInstall = 24,

    /// <summary>
    /// View all installed BYOND versions
    /// </summary>
    InstanceByondListInstalled = 25,

    /// <summary>
    /// View the information for DreamMaker
    /// </summary>
    InstanceDMRead = 26,

    /// <summary>
    /// View the DreamMaker event log
    /// </summary>
    InstanceDMInvestigate = 27,

    /// <summary>
    /// Start, or cancel, active deployments
    /// </summary>
    InstanceDMDeploy = 28,

    /// <summary>
    /// Modify the settings for DreamMaker
    /// </summary>
    InstanceDMConfiguration = 29,

    /// <summary>
    /// View all previous deployments and compile job history
    /// </summary>
    InstanceDMJobs = 30,

    /// <summary>
    /// Change the Security Level of DreamMaker
    /// </summary>
    InstanceDMSecurityLevel = 31,

    /// <summary>
    /// Change the API Validation requirements
    /// </summary>
    InstanceDMApi = 32,

    /// <summary>
    /// Change how long the API takes to validate before assuming a crash
    /// </summary>
    InstanceDMTimeout = 33,
  }
}
