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
  public string RootID { get; set; } = string.Empty;

  [Column("enabled"), Required, DefaultValue(false)]
  public bool Enabled { get; set; }

  [Column("dd_address"), Required, DefaultValue("127.0.0.1")]
  public string DreamDaemonAddress { get; set; } = "127.0.0.1";

  [Column("dd_port"), Required, DefaultValue(2577)]
  public int DreamDaemonPort { get; set; } = 2577;

  [Column("dd_trust_level"), Required, DefaultValue(TrustLevel.Trusted)]
  public int DreamDaemonTrustLevel { get; set; } = TrustLevel.Trusted;

  [Column("dd_visibility"), Required, DefaultValue(Visibility.Invisible)]
  public int DreamDaemonVisibility { get; set; } = Visibility.Invisible;

  [Column("dd_params"), Required, DefaultValue("")]
  public string DreamDaemonParams { get; set; } = "";

  [Column("dd_version"), Required, DefaultValue("")]
  public string DreamMakerVersion { get; set; } = "";

  [Column("dd_api_validate"), Required, DefaultValue(false)]
  public bool DreamMakerApiValidation { get; set; }

  /// <summary>
  /// This is in seconds
  /// </summary>
  [Column("dd_heartbeat_interval"), Required, DefaultValue(600)]
  public int DreamDaemonHeartbeatInterval { get; set; } = 600;

  [Column("dd_psm_key"), Required, DefaultValue("change_me_idiot")]
  public string DreamDaemonPSMKey { get; set; } = "change_me_idiot";

  [Column("dd_dme_name"), Required, DefaultValue("default_dme")]
  public string DreamDaemonDmeName { get; set; } = "default_dme";

  /// <summary>
  /// In seconds
  /// </summary>
  [Column("dd_deploy_timeout"), Required, DefaultValue(600)]
  public int DreamDaemonDeployTimeoutLength { get; set; } = 600;

  [Column("dd_active_deployment"), Required]
  public Guid DreamDaemonDeployActive { get; set; } = Guid.Empty;

  [Column("dd_target_deployment"), Required]
  public Guid DreamDaemonDeployTarget { get; set; } = Guid.Empty;

  [Column("dd_graceful"), Required, DefaultValue(0)]
  public int DreamDaemonGraceful { get; set; } = GracefulActions.NoAction;
}
