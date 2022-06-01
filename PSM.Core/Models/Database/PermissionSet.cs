using System.ComponentModel.DataAnnotations.Schema;
using PSM.Core.Core;

namespace PSM.Core.Models.Database;

[Table("permission_sets")]
public class PermissionSet {
  public int Id { get; set; }

  public string PermissionString {
    get => _permissionMap.ConvertToPermissionString();
    set => _permissionMap = value.ConvertToPermissionList();
  }

  public bool                Contains(PSMPermission permission) => _permissionMap.Contains(permission);
  public void                Add(PSMPermission      permission) => _permissionMap.Add(permission);
  public void                Remove(PSMPermission   permission) => _permissionMap.Remove(permission);
  public List<PSMPermission> AsList()                           => _permissionMap;

  [NotMapped]
  private List<PSMPermission> _permissionMap = new();

  [NotMapped]
  public User? UserOwner { get; set; }
}
