namespace PSM.Core.Models.API;

public class UserUpdateModel {
  public string username    { get; set; }
  public bool   enabled     { get; set; }
  public string permissions { get; set; }
}
