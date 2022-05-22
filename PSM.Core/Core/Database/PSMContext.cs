using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Models;
using PSM.Core.Models.Database;

namespace PSM.Core.Core.Database {
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class PSMContext : DbContext {
    public PSMContext(DbContextOptions<PSMContext> options) : base(options) { }
    public DbSet<User>          Users          { get; set; }
    public DbSet<PermissionSet> PermissionSets { get; set; }
    public User                 SystemUser;

    /// <summary>
    /// Adds the default system user and an admin user
    /// </summary>
    public bool SeedPSMUsers(ILogger logger) {
      var seedingOccured = false;
      // Check for the system user and generate if not found
      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemUsername)) is not { } systemUser) {
        systemUser = new User {
                                Enabled      = false,
                                Username     = Constants.System.SystemUsername,
                                PasswordHash = "_" // Impossible to get via hashes
                              };
        systemUser.PermissionSet = new PermissionSet { Id = systemUser.Id, PermissionString = Constants.AllPermissions };
        Users.Add(systemUser);
        PermissionSets.Add(systemUser.PermissionSet);
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

      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemAdminUsername)) is null) {
        var adminUser = new User {
                                   Enabled   = true,
                                   Username  = Constants.System.SystemAdminUsername,
                                   CreatedBy = systemUser
                                 };
        adminUser.PasswordHash  = new PasswordHasher<User>().HashPassword(adminUser, Constants.System.SystemAdminPassword);
        adminUser.PermissionSet = new PermissionSet { Id = adminUser.Id, PermissionString = Constants.AllPermissions };
        CreatePSMUser(systemUser, adminUser);
        logger.LogInformation("Created default admin user");
        seedingOccured = true;
        SaveChanges();
      }

      SaveChanges();
      return seedingOccured;
    }

    public PSMResponse CreatePSMUser(User initiator, User toCreate) {
      if(!CheckPermission(initiator, PSMPermission.UserCreate))
        return PSMResponse.NoPermission;
      // Check that all of the created users permissions are possessed by the initiator
      if(!toCreate.PermissionSet.AsList().All(initiator.PermissionSet.AsList().Contains))
        return PSMResponse.NoPermission;
      Users.Add(toCreate);
      toCreate.PermissionSet.MapOwner(this);
      PermissionSets.Add(toCreate.PermissionSet);
      SaveChanges();
      return PSMResponse.Ok;
    }

    public bool CheckPermission(User user, PSMPermission permission) {
      if(user.PermissionSet is not null)
        return user.PermissionSet.Contains(permission);
      // If their permission set doesnt exist attempt to locate their set in the db, or create one if not found
      if(PermissionSets.FirstOrDefault(set => set.Id == user.Id) is not { } userSet) {
        userSet = new PermissionSet { Id = user.Id, PermissionString = "" };
        PermissionSets.Add(userSet);
        SaveChanges();
      }

      userSet.MapOwner(this);
      return CheckPermission(user, permission);
    }

    public PSMResponse PermissionGrant(User initiator, User user, PSMPermission permission) {
      if(!CheckPermission(initiator, permission) || !CheckPermission(initiator, PSMPermission.UserModify))
        return PSMResponse.NoPermission;
      if(user.PermissionSet.Contains(permission))
        return PSMResponse.NotFound;

      user.PermissionSet.Add(permission);
      SaveChanges();
      return PSMResponse.Ok;
    }

    public PSMResponse PermissionDeny(User initiator, User user, PSMPermission permissions) {
      if(!CheckPermission(initiator, permissions) || !CheckPermission(initiator, PSMPermission.UserModify))
        return PSMResponse.NoPermission;
      if(!user.PermissionSet.Contains(permissions))
        return PSMResponse.NotFound;

      user.PermissionSet.Remove(permissions);
      SaveChanges();
      return PSMResponse.Ok;
    }
  }
}
