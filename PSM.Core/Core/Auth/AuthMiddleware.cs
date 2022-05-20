using Microsoft.AspNetCore.Mvc;

namespace PSM.Core.Core.Auth {
    public class AuthMiddleware {
        private readonly RequestDelegate next;
        public AuthMiddleware(RequestDelegate _next) {
            next = _next;
        }

        public async Task Invoke(HttpContext context, IAuthService auth) {
            // Skip the entire validation step if they didnt give us a token
            if(!context.Request.Headers["Authorization"].Contains("Bearer")) {
                await next(context);
                return;
            }

            string token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split("Bearer ").Last();

            if (token != null) {
                auth.ValidateJWT(context, token);
            }

            await next(context);
        }
    }
}
