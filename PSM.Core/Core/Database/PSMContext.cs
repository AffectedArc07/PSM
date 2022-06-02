using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Models.Database;

namespace PSM.Core.Core.Database {
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class PSMContext : DbContext {
    public PSMContext(DbContextOptions<PSMContext> options) : base(options) { }
    public DbSet<User>          Users          { get; set; } = null!;
    public DbSet<PermissionSet> PermissionSets { get; set; } = null!;
    public DbSet<UserToken>     UserTokens     { get; set; } = null!;

    public User? GetUser(int userID, bool includeDisabled = false) {
      var user = Users.Find(userID);
      if(user is null)
        return null;
      if(user.Disabled && !includeDisabled)
        return null;
      user.PermissionSet = PermissionSets.Find(userID) ?? PermissionSets.Add(new PermissionSet { Id = userID, PermissionString = "", UserOwner = user }).Entity;
      SaveChanges();
      return user;
    }

    public User SystemUser = null!, AdminUser = null!;

    public void MapAllUsers() {
      foreach(var user in Users.ToList())
        user.PermissionSet = PermissionSets.Find(user.Id) ??
                             PermissionSets.Add(new PermissionSet { Id = user.Id, PermissionString = "", UserOwner = user }).Entity;
      if(SystemUser.PermissionSet.PermissionString != Constants.AdminPermissionString)
        SystemUser.PermissionSet.PermissionString = Constants.AdminPermissionString;
      if(AdminUser.PermissionSet.PermissionString != Constants.AdminPermissionString)
        AdminUser.PermissionSet.PermissionString = Constants.AdminPermissionString;
      if(SystemUser.Enabled) SystemUser.Enabled = false;
      SaveChanges();
    }

    /// <summary>
    /// Adds the default system user and an admin user
    /// </summary>
    public bool SeedPSMUsers(ILogger logger) {
      var seedingOccured = false;
      // Check for the system user and generate if not found
      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemUsername)) is not { } systemUser) {
        // ReSharper disable once UseObjectOrCollectionInitializer
        systemUser = new User {
                                Enabled      = false,
                                Username     = Constants.System.SystemUsername,
                                PasswordHash = "_", // Impossible to get via hashes
                              };
        systemUser.PermissionSet = new PermissionSet { PermissionString = Constants.AdminPermissionString };
        Users.Add(systemUser);
        PermissionSets.Add(systemUser.PermissionSet);
        logger.LogInformation("Created system user");
        seedingOccured = true;
        SaveChanges();
      }

      if(Users.FirstOrDefault(user => user.Username.Equals(Constants.System.SystemAdminUsername)) is not { } adminUser) {
        adminUser = new User {
                               Enabled   = true,
                               Username  = Constants.System.SystemAdminUsername,
                               CreatedBy = systemUser,
                             };
        adminUser.PasswordHash = new PasswordHasher<User>().HashPassword(adminUser, Constants.System.SystemAdminPassword);
        CreatePSMUser(systemUser, adminUser);
        adminUser.PermissionSet.PermissionString = Constants.AdminPermissionString;
        logger.LogInformation("Created default admin user");
        seedingOccured = true;
        SaveChanges();
      }

      SystemUser = systemUser;
      AdminUser  = adminUser;

      return seedingOccured;
    }

    /// <summary>
    /// Creates a new user with the information provided in the toCreate parameter
    /// The userID will automatically be adjusted to be the next valid userID
    /// </summary>
    public PSMResponse CreatePSMUser(User initiator, User toCreate) {
      if(!CheckPermission(initiator, PSMPermission.UserCreate))
        return PSMResponse.NoPermission;
      if(Users.Any(dbUser => dbUser.Username.ToLower().Equals(toCreate.Username)))
        return PSMResponse.Conflict;
      toCreate.Id = Users.Max(dbUser => dbUser.Id) + 1;
      Users.Add(toCreate);
      SaveChanges();
      PermissionSets.Add(toCreate.PermissionSet = new PermissionSet {
                                                                      Id               = toCreate.Id,
                                                                      PermissionString = "",
                                                                      UserOwner        = toCreate
                                                                    });
      SaveChanges();
      return PSMResponse.Ok;
    }

    public bool CheckPermission(User user, PSMPermission permission) => user.PermissionSet.Contains(permission);

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
