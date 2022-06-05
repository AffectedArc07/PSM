using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Core.Database.Tables;

namespace PSM.Core.Core.Database;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PermissionContext : DbContext {
  public PermissionContext(DbContextOptions<PermissionContext> options, UserContext userContext, InstanceContext instanceContext) : base(options) {
    _userContext     = userContext;
    _instanceContext = instanceContext;
    _userContext.WithPermissionContext(this);
  }

  private readonly UserContext     _userContext;
  private readonly InstanceContext _instanceContext;

  protected DbSet<GlobalPermissionSet>   GlobalSets   { get; set; } = null!;
  protected DbSet<InstancePermissionSet> InstanceSets { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<InstancePermissionSet>().HasKey(ips => new { ips.UserID, ips.InstanceID });
  }

  public async Task<GlobalPermissionSet> GetGlobalSet(int userID) {
    if(await _userContext.GetUser(userID, false) is not { } dbUser) throw new KeyNotFoundException();
    if(await GlobalSets.FindAsync(userID) is not { } dbSet) {
      dbSet = new GlobalPermissionSet {
                                        UserID           = userID,
                                        PermissionString = "",
                                      };
      await GlobalSets.AddAsync(dbSet);
      await SaveChangesAsync();
    }

    dbSet.UserOwner = dbUser;
    return dbSet;
  }

  public async Task<InstancePermissionSet> GetInstanceSet(int instanceID, int userID) {
    if(await _userContext.GetUser(userID) is not { } dbUser) throw new KeyNotFoundException();
    if(await _instanceContext.GetInstance(instanceID) is not { } dbInstance) throw new KeyNotFoundException();
    if(await InstanceSets.FindAsync(userID) is not { } dbSet) {
      dbSet = new InstancePermissionSet {
                                          UserID           = userID,
                                          InstanceID       = instanceID,
                                          PermissionString = "",
                                        };
      await InstanceSets.AddAsync(dbSet);
      await SaveChangesAsync();
    }

    dbSet.UserOwner     = dbUser;
    dbSet.InstanceOwner = dbInstance;
    return dbSet;
  }
}
