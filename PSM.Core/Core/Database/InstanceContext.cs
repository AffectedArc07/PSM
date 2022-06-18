using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Database.Tables;

namespace PSM.Core.Database;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class InstanceContext : DbContext {
  protected DbSet<Instance> Instances { get; set; } = null!;

  private readonly UserContext _userContext;

  public InstanceContext(DbContextOptions<InstanceContext> options, UserContext userContext, PermissionContext permissionContext) : base(options) {
    _userContext = userContext.WithPermissionContext(permissionContext);
  }

  public async Task<Instance?> GetInstance(int instanceID) => await Instances.FindAsync(instanceID);
}
