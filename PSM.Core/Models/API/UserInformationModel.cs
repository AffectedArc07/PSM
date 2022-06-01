namespace PSM.Core.Models.API;

public class UserInformationModel : ModelBase {
  public string? Username { get; set; }
  public int?    UserID   { get; set; } = -1;
  public bool?   Enabled  { get; set; }
}
