namespace PSM.Core.Models.Auth;

public class ClientLoginResponseModel : ModelBase {
  public ClientModel?      Client;
  public ClientTokenModel? TokenResponse;

  protected override bool ValidateModel() => Client is { } && TokenResponse is { };
}
