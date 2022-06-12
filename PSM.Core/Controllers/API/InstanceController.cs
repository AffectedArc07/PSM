using Microsoft.AspNetCore.Mvc;
using PSM.Core.Database;

namespace PSM.Core.Controllers.API;

[Route("api/instance")]
public class InstanceController : Controller {
  private readonly InstanceContext _instanceContext;
  private readonly UserContext     _userContext;

  public InstanceController(InstanceContext instanceContext, UserContext userContext, PermissionContext permissionContext) {
    _userContext     = userContext.WithPermissionContext(permissionContext);
    _instanceContext = instanceContext;
  }

  [HttpGet("{instanceID:int}")]
  public async Task<IActionResult> GetInstanceData(int instanceID) {
    if(await _instanceContext.GetInstance(instanceID) is not { } instance)
      return NotFound();
    return Ok(instance.GetInformationModel());
  }
}
