using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Core.Database.Tables;

namespace PSM.Core.Core.Database;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class UserContext : DbContext {
  public UserContext(DbContextOptions<UserContext> options) : base(options) { }

  public UserContext WithPermissionContext(PermissionContext psmContext) {
    _permissionContext = psmContext;
    return this;
  }

  public override int SaveChanges() {
    _permissionContext?.SaveChanges();
    return base.SaveChanges();
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new()) {
    await(_permissionContext?.SaveChangesAsync(cancellationToken) ?? Task.CompletedTask);
    return await base.SaveChangesAsync(cancellationToken);
  }

  public User SystemUser { get; private set; } = null!;
  public User AdminUser  { get; private set; } = null!;

  protected DbSet<User>      Users  { get; set; } = null!;
  protected DbSet<UserToken> Tokens { get; set; } = null!;

  private PermissionContext? _permissionContext;

  public async Task<User?> GetUserByUsername(string username) => await Users.FirstOrDefaultAsync(dbUser => dbUser.Username.Equals(username));

  public async Task<User?> GetUser(int userID, bool populatePermissions = true) {
    if(await Users.FindAsync(userID) is not { } dbUser) return null;
    if(populatePermissions) dbUser.GlobalPermissionSet = await _permissionContext.GetGlobalSet(userID);
    return dbUser;
  }

  public async Task<User[]> GetAllUsers() {
    return await Users.ToArrayAsync();
  }

  public async Task<User> CreateUser(string username, User? creator = null, int? userIdOverride = null) {
    var nextID = userIdOverride ?? Users.Max(dbUser => dbUser.Id) + 1;
    var user = new User {
                          Archived     = false,
                          CreatedBy    = creator?.Id,
                          Enabled      = false,
                          Id           = nextID,
                          PasswordHash = "_",
                          Username     = username
                        };
    await Users.AddAsync(user);
    await SaveChangesAsync();
    user.GlobalPermissionSet = await _permissionContext.GetGlobalSet(nextID);
    return user;
  }

  public async Task<UserToken> GetToken(int userID) {
    return await Tokens.FindAsync(userID) ??
           (await Tokens.AddAsync(new UserToken {
                                                  ExpiresAt = DateTimeOffset.Now.AddTicks(-1),
                                                  UserID    = userID
                                                })).Entity;
  }

  public async Task EnsureDefaultUsers() {
    if(await GetUser(Constants.System.SystemUserID) is not { } sysUser)
      sysUser = await CreateUser(Constants.System.SystemUsername, userIdOverride: Constants.System.SystemUserID);
    if(await GetUser(Constants.System.AdminUserID) is not { } admUser) {
      admUser              = await CreateUser(Constants.System.SystemAdminUsername, userIdOverride: Constants.System.AdminUserID);
      admUser.PasswordHash = new PasswordHasher<User>().HashPassword(admUser, Constants.System.SystemAdminPassword);
      admUser.Enabled      = true;
      await SaveChangesAsync();
    }

    (await _permissionContext.GetGlobalSet(sysUser.Id)).PermissionString = Constants.AdminPermissionString;
    (await _permissionContext.GetGlobalSet(admUser.Id)).PermissionString = Constants.AdminPermissionString;
    await _permissionContext.SaveChangesAsync();

    SystemUser = sysUser;
    AdminUser  = admUser;
  }

  public async Task<UserToken?> GetTokenFromValue(string auth) => await Tokens.FirstOrDefaultAsync(dbToken => dbToken.TokenValue.Equals(auth));
}
