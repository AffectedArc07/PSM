namespace PSM.Core.Models.Auth;

public class ClientLoginModel : ModelBase {
  public string username;
  public string password;

  protected override bool ValidateModel() {
    if(password.Length < 8)
      return false;
    if(username.Length < 8)
      return false;
    
    return true;
  }
}
