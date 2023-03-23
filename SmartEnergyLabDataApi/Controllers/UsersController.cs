using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("Users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        IHubContext<NotificationHub> _hubContext;
        public UsersController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        [Route("SaveNewUser")]
        public IActionResult SaveNewUser([FromBody] NewUser newUser) {
            using( var m = new NewUserModel(this,newUser) ) {
                if ( !m.Save() ) {
                    return this.ModelErrors(m.Errors);
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("Logon")]
        public IActionResult Logon([FromBody] Logon logon) {
            using( var m = new LogonModel(this, logon) ) {
                if ( !m.TryLogon() ) {
                    return this.ModelErrors(m.Errors);
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("Logoff")]
        public IActionResult Logoff() {
            this.LogOffNow();
            return Ok();
        }

        [HttpGet]
        [Route("CurrentUser")]
        public User CurrentUser() {
            using( var m = new CurrentUserModel(this) ) {
                return (m.User);
            }
        }

        [HttpPost]
        [Route("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePassword changePassword) {
            using( var m = new ChangePasswordModel(this, changePassword) ) {
                if ( !m.Save() ) {
                    return this.ModelErrors(m.Errors);
                }
            }
            return Ok();
        }
    }
}
