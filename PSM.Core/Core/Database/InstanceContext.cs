﻿using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Database.Tables;

namespace PSM.Core.Database;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class InstanceContext : DbContext {
  private readonly UserContext _userContext;

  public InstanceContext(DbContextOptions<InstanceContext> options, UserContext userContext) : base(options) {
    _userContext = userContext;
  }

  protected DbSet<Instance> Instances { get; set; } = null!;

  public async Task<Instance?> GetInstance(int instanceID) => await Instances.FindAsync(instanceID);
}
