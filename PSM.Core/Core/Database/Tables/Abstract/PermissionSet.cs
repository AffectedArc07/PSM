using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PSM.Core.Core.Database.Tables.Abstract;

public abstract class PermissionSet {
  [Key, Required, Column(Order = 0), DefaultValue(1)]
  public int UserID { get; init; } = 1;

  [Required, DefaultValue("")]
  public string PermissionString { get; set; } = "";

  [NotMapped]
  public User UserOwner { get; set; } = null!;

  [NotMapped]
  private int? _lastSplitHash;

  [NotMapped]
  private IReadOnlyList<PSMPermission>? _lastSplit;

  public IReadOnlyList<PSMPermission> AsList() {
    if(_lastSplitHash is { } && _lastSplit is { } && _lastSplitHash == PermissionString.GetHashCode())
      return _lastSplit;

    var list = new List<PSMPermission>();
    if(string.IsNullOrWhiteSpace(PermissionString))
      return list;

    var split = PermissionString.Trim().Trim(';').Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
    if(split.Length == 0)
      return list;

    try {
      list.AddRange(split.Select(Enum.Parse<PSMPermission>).Distinct());
      list.Sort();
    } catch(FormatException fEx) {
      Constants.AppLog.LogCritical("Failed to parse permission string");
      Constants.AppLog.LogCritical(" Inner: {FormatException}", fEx.ToString());
    }

    _lastSplitHash = PermissionString.GetHashCode();
    _lastSplit     = list.AsReadOnly();
    return _lastSplit;
  }

  public void FromList(IEnumerable<PSMPermission> perms) {
    var p = perms.ToArray();
    if(!p.All(ValidPermissions.Contains)) throw new InvalidOperationException("List contains permission that is not valid for this permission set");
    PermissionString = PermissionListToString(p);
  }

  public static string PermissionListToString(IEnumerable<PSMPermission> perms) => perms.ToList().Select(p => (int)p).Distinct().OrderBy(p => p).Aggregate("", (s, p) => $"{s};{p}").Trim(';');

  public bool CheckPermission(PSMPermission permission) {
    if(!ValidPermissions.Contains(permission)) throw new InvalidOperationException($"{permission} is not valid for this permission set");
    return AsList().Contains(permission);
  }

  public void AddPermission(PSMPermission permission) {
    if(!ValidPermissions.Contains(permission)) throw new InvalidOperationException($"{permission} is not valid for this permission set");
    if(CheckPermission(permission))
      return;
    PermissionString = PermissionListToString(AsList().Append(permission));
  }

  public void RemovePermission(PSMPermission permission) {
    if(!ValidPermissions.Contains(permission)) throw new InvalidOperationException($"{permission} is not valid for this permission set");
    if(!CheckPermission(permission))
      return;
    PermissionString = PermissionListToString(AsList().Where(p => p != permission));
  }

  public void AddPermissionRange(params PSMPermission[] perms) {
    foreach(var perm in perms)
      AddPermission(perm);
  }

  public void RemovePermissionRange(params PSMPermission[] perms) {
    foreach(var perm in perms)
      RemovePermission(perm);
  }

  [NotMapped]
  protected abstract IEnumerable<PSMPermission> ValidPermissions { get; }
}
