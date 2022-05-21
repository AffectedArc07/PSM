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

      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemAdminUsername, StringComparison.Ordinal)) is not { } adminUser) {
        adminUser = new User {
                               Enabled   = true,
                               Username  = Constants.System.SystemAdminUsername,
                               CreatedBy = systemUser
                             };
        adminUser.PasswordHash  = new PasswordHasher<User>().HashPassword(adminUser, Constants.System.SystemAdminPassword);
        adminUser.PermissionSet = new PermissionSet { Owner = adminUser.Id, Permissions = Constants.AllPermissions };
        Users.Add(adminUser);
        adminUser.PermissionSet.MapOwner(this);
        logger.LogInformation("Created default admin user");
        seedingOccured = true;
        SaveChanges();
      }

      SaveChanges();
      return seedingOccured;
    }
  }
}
