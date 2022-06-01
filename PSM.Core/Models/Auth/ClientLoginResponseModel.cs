namespace PSM.Core.Models.Auth;

public class ClientLoginResponseModel : ModelBase {
  public ClientModel?      Client;
  public ClientTokenModel? TokenResponse;

  public override bool ValidateModel() => Client is { } && TokenResponse is { };
}
