using Newtonsoft.Json;

namespace PSM.Core.Models.Auth;

public class ClientModel : ModelBase {
  [JsonRequired]
  public string? Username, Password;

  /// <summary>
  /// The 'hash' of the supplied Username and Password.
  /// Despite being called a hash it actually needs to be the length of Username and Password concatenated.
  /// </summary>
  [JsonRequired]
  public int? CredentialHash;

  /// <summary>
  ///  This is not required for the Model; but should be set if available.
  /// </summary>
  public ulong? UserID;

  public override bool ValidateModel() {
    if(Username is null || Password is null || CredentialHash is null)
      return false;

    if(Username.Length < 8)
      return false;

    if(Password.Length < 8)
      return false;
    // Check for any illegal username characters
    if(Username.Any(digit => !char.IsLetterOrDigit(digit)))
      return false;

    if(UserID is 0)
      return false;

    if(CredentialHash != string.Concat(Username, Password).Length)
      return false;

    return true;
  }
}
