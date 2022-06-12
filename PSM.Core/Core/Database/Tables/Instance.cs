using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace PSM.Core.Database.Tables;

[Table("instances"), UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Instance {
  [Column("id", Order = 0), Key, Required]
  public int Id { get; set; }

  [Column("name"), Required, MinLength(4), MaxLength(32), DefaultValue("NameNotSet")]
  public string Name { get; set; } = "NameNotSet";

  [Column("root"), Required, DefaultValue("")]
  public string RootPath { get; set; } = string.Empty;

  [Column("enabled"), Required, DefaultValue(false)]
  public bool Enabled { get; set; }
}
