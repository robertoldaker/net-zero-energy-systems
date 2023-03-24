using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Controllers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{

    public class NewUserModel : DbModel {

        private NewUser _newUser;
        public NewUserModel(ControllerBase c, NewUser newuser) : base(c) {
            _newUser = newuser;
        }

        protected override void checkModel()
        {            
            if ( string.IsNullOrEmpty(_newUser.Email) ) {
                addError("email","Field is mandatory");
            } else {
                var checker = new EmailChecker();
                if ( !checker.Check(_newUser.Email)) {
                    addError("email","Invalid email address");
                } else if ( _da.Users.ContainsUser(_newUser.Email,0) ) {
                    addError("email","User with this email already exists");
                }
            }
            if ( string.IsNullOrEmpty(_newUser.Name) ) {
                addError("name","Field is mandatory");
            }
            if ( _newUser.Password != _newUser.ConfirmPassword ) {
                addError("password","Fields must match");
                addError("confirmPassword","Fields must match");
            }
        }

        protected override void beforeSave()
        {
            var user = new User();
            user.Email = _newUser.Email;
            user.Name = _newUser.Name;
            user.SetPassword(_newUser.Password);
            user.Enabled = true;
            _da.Users.Add(user);
            // also add an Elsi data version
            var dv = new ElsiDataVersion();
            dv.User = user;
            dv.Name = "Default";
            _da.Elsi.Add(dv);
        }
    }

    public class Logon {
        public string Email {get; set;}
        public string Password {get; set;}
    }

    public class LogonModel : DbModel
    {
        private User _user;
        private string _password;
        public LogonModel(ControllerBase c, Logon logon) : base(c) {
            _user = _da.Users.GetUser(logon.Email);
            _password = logon.Password;
        }

        public bool TryLogon() {
            if ( _user!=null ) {
                if ( _user.VerifyPassword(_password) ) {
                    logon();
                    return true;
                } else {
                    addError("password","Authentication failed");
                    return false;
                }
            } else {
                addError("email","No email found");
                return false;
            }
        }

        private void logon() {
            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, _user.Id.ToString()),
                    };

            var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
            _c.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal, new AuthenticationProperties()
            {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow + new TimeSpan(14, 0, 0, 0),
            }).Wait();
        }

    }

    public class CurrentUserModel : DbModel {

        private User _user;
        public CurrentUserModel(ControllerBase c) : base(c) {
            var userId = c.GetUserId();
            if ( userId!=0 ) {
                _user = _da.Users.GetUser(userId);
            }
        }

        public User User {
            get {
                return _user;
            }
        }

    }

    public class ChangePassword {
        public string Password {get; set;}
        public string NewPassword1 {get; set;}
        public string NewPassword2 {get; set;}
    }

    public class ChangePasswordModel : DbModel
    {
        private User _user;
        private ChangePassword _changePassword;
        public ChangePasswordModel(ControllerBase c, ChangePassword changePassword) : base(c) {
            _changePassword = changePassword;
            _user = _da.Users.GetUser(c.GetUserId());
        }

        protected override void checkModel()
        {
            //
            if ( _user==null ) {
                throw new Exception("User not logged in");
            }
            if ( !_user.VerifyPassword(_changePassword.Password)) {
                this.addError("password","Authentication error");
            }
            if ( _changePassword.NewPassword1!=_changePassword.NewPassword2) {
                this.addError("newPassword1","Passwords do not match");
                this.addError("newPassword2","Passwords do not match");
            }
        }

        protected override void beforeSave()
        {
            _user.SetPassword(_changePassword.NewPassword1);
        }
    }

    public class UsersModel : DbModel {
        public UsersModel(ControllerBase c) : base(c) {

        }

        public IList<User> GetUsers() {
            return _da.Users.GetUsers();
        }
    }

}