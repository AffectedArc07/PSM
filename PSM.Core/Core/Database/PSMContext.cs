using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Models;

namespace PSM.Core.Core.Database {
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class PSMContext : DbContext {
    public PSMContext(DbContextOptions<PSMContext> options) : base(options) { }
    public DbSet<User>          Users          { get; set; } = null!;
    public DbSet<PermissionSet> PermissionSets { get; set; } = null!;
    public User                 SystemUser;

    /// <summary>
    /// Adds the default system user and an admin user
    /// </summary>
    public bool SeedPSMUsers(ILogger logger) {
      var seedingOccured = false;
      // Check for the system user and generate if not found
      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemUsername, StringComparison.Ordinal)) is not { } systemUser) {
        systemUser = new User {
                                Enabled      = false,
                                Username     = Constants.System.SystemUsername,
                                PasswordHash = "_" // Impossible to get via hashes
                              };
        systemUser.PermissionSet = new PermissionSet { Owner = systemUser.Id, Permissions = Constants.AllPermissions };
        Users.Add(systemUser);
        systemUser.PermissionSet.MapOwner(this);
        logger.LogInformation("Created system user");
        seedingOccured = true;
        SaveChanges();
      }

      if(systemUser.Enabled) {
        // This should never be enabled
        logger.LogCritical("System User is enabled");
        Environment.Exit(Constants.ExitCodes.SystemUserEnabled);
        return false;
      }

      SystemUser = systemUser;

      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemAdminUsername, StringComparison.Ordinal)) is not { } adminUser) {
        adminUser = new User {
                               Enabled   = true,
                               Username  = Constants.System.SystemAdminUsername,
                               CreatedBy = systemUser
                             };
        adminUser.PasswordHash  = new PasswordHasher<User>().HashPassword(adminUser, Constants.System.SystemAdminPassword);
        adminUser.PermissionSet = new PermissionSet { Owner = adminUser.Id, Permissions = Constants.AllPermissions };
        CreatePSMUser(systemUser, adminUser);
        logger.LogInformation("Created default admin user");
        seedingOccured = true;
        SaveChanges();
      }

      SaveChanges();
      return seedingOccured;
    }

    public PSMResponse CreatePSMUser(User initiator, User toCreate) {
      if(!initiator.PermissionSet.Permissions.Contains(PSMPermissions.UserCreate))
        return PSMResponse.NoPermission;
      // Check that all of the created users permissions are possessed by the initiator
      if(!toCreate.PermissionSet.Permissions.All(initiator.PermissionSet.Permissions.Contains))
        return PSMResponse.NoPermission;
      Users.Add(toCreate);
      toCreate.PermissionSet.MapOwner(this);
      PermissionSets.Add(toCreate.PermissionSet);
      SaveChanges();
      return PSMResponse.Ok;
    }

    public bool CheckPermission(User user, PSMPermissions permission) {
      if(user.PermissionSet is not null)
        return user.PermissionSet.Permissions.Contains(permission);
      // If their permission set doesnt exist attempt to locate their set in the db, or create one if not found
      if(PermissionSets.FirstOrDefault(set => set.Owner == user.Id) is not { } userSet) {
        userSet = new PermissionSet { Owner = user.Id, Permissions = new List<PSMPermissions>() };
        PermissionSets.Add(userSet);
        SaveChanges();
      }

      userSet.MapOwner(this);
      return CheckPermission(user, permission);
    }

    public PSMResponse PermissionGrant(User initiator, User user, PSMPermissions permission) {
      if(!CheckPermission(initiator, permission) || !CheckPermission(initiator, PSMPermissions.UserModify))
        return PSMResponse.NoPermission;
      if(user.PermissionSet.Permissions.Contains(permission))
        return PSMResponse.NotFound;

      user.PermissionSet.Permissions.Add(permission);
      SaveChanges();
      return PSMResponse.Ok;
    }

    public PSMResponse PermissionDeny(User initiator, User user, PSMPermissions permissions) {
      if(!CheckPermission(initiator, permissions) || !CheckPermission(initiator, PSMPermissions.UserModify))
        return PSMResponse.NoPermission;
      if(!user.PermissionSet.Permissions.Contains(permissions))
        return PSMResponse.NotFound;

      user.PermissionSet.Permissions.Remove(permissions);
      SaveChanges();
      return PSMResponse.Ok;
    }
  }
}
