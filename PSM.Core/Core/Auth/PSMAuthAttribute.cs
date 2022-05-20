using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PSM.Core.Models;

namespace PSM.Core.Core.Auth {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PSMAuthAttribute : Attribute, IAuthorizationFilter {
        public void OnAuthorization(AuthorizationFilterContext context) {
            User user = (User) context.HttpContext.Items["User"];
            if (user == null) {
                // not logged in
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
}
