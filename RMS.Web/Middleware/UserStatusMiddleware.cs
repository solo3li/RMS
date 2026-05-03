using Microsoft.AspNetCore.Identity;
using RMS.Web.Models;

namespace RMS.Web.Middleware
{
    public class UserStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                
                if (user == null || !user.IsActive)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/Login?error=AccountSuspended");
                    return;
                }

                // Update LastLogin periodically (e.g., every 5 minutes to avoid DB spam)
                if (user.LastLogin == null || (DateTime.Now - user.LastLogin.Value).TotalMinutes > 5)
                {
                    user.LastLogin = DateTime.Now;
                    await userManager.UpdateAsync(user);
                }
            }

            await _next(context);
        }
    }
}
