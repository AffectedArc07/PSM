namespace PSM.Core.Models.Server; 

public class ServerInfoModel : ModelBase {
  public string? ServerVersion, CurrentUsername;

  public override bool ValidateModel() => true; // We don't do any validation here.
}
