using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SmartEnergyLabDataApi.Controllers
{
    public static class ControllerBaseMethods {
        public static IActionResult ModelErrors(this ControllerBase c, Dictionary<string,string> errors) {
            return c.StatusCode(422,errors);
        }

        public static int GetUserId(this ControllerBase c)
        {
            int userId;
            if (c.IsAuthenticated() && int.TryParse(c.User.Identity.Name, out userId))
            {
                return userId;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsAuthenticated(this ControllerBase controller)
        {
            return controller.User.Identity.IsAuthenticated;
        }

        public static void LogOffNow(this ControllerBase c, string? connectionId)
        {
            //
            int userId = c.GetUserId();
            //
            c.HttpContext.SignOutAsync().Wait();
        }

    }
}
