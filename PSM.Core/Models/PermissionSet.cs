using System.ComponentModel.DataAnnotations.Schema;
using PSM.Core.Core;
using PSM.Core.Core.Database;

namespace PSM.Core.Models;

public class PermissionSet {
  public int                  Owner       { get; set; }
  public List<PSMPermissions> Permissions { get; set; }

  [NotMapped]
  public User? UserOwner { get; set; }

  public void MapOwner(PSMContext context) {
    var owner = context.Users.FirstOrDefault(user => user.Id == Owner);
    if(owner is null)
      return;
    UserOwner               = owner;
    UserOwner.PermissionSet = this;
  }
}
