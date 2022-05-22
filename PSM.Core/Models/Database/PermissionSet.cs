using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using PSM.Core.Core;
using PSM.Core.Core.Database;
using PSM.Core.Models.Database;

namespace PSM.Core.Models;

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

  private List<PSMPermission> _permissionMap = new();

  [NotMapped]
  public User? UserOwner { get; set; }

  public void MapOwner(PSMContext context) {
    var owner = context.Users.FirstOrDefault(user => user.Id == Id);
    if(owner is null)
      return;
    UserOwner               = owner;
    UserOwner.PermissionSet = this;
  }
}
