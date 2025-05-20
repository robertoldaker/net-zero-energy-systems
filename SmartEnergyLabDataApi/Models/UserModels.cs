using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.ObjectPool;
using NHibernate.Mapping.Attributes;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Tls;
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
            if ( string.IsNullOrEmpty(_newUser.Password)) {
                addError("password","Field is mandatory");
            }
            if ( string.IsNullOrEmpty(_newUser.ConfirmPassword)) {
                addError("confirmPassword","Field is mandatory");
            }
            if ( !string.IsNullOrEmpty(_newUser.Password) && !string.IsNullOrEmpty(_newUser.ConfirmPassword)) {
                if (_newUser.Password != _newUser.ConfirmPassword ) {
                    addError("password","Fields must match");
                    addError("confirmPassword","Fields must match");
                }
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
        }
    }

    public class Logon {
        public string Email {get; set;}
        public string Password {get; set;}
    }

    public class LogonModel : DbModel
    {
        Logon _logon;
        private string _password;
        public LogonModel(ControllerBase c, Logon logon) : base(c) {
            _logon = logon;
            _password = logon.Password;
        }

        public bool TryLogon() {
            bool result = false;
            if ( string.IsNullOrEmpty(_logon.Email)) {
                addError("email","Field is mandatory");
            }
            if ( string.IsNullOrEmpty(_logon.Password)) {
                addError("password","Field is mandatory");
            }
            if ( !string.IsNullOrEmpty(_logon.Email) && !string.IsNullOrEmpty(_logon.Password) ) {
                var user = _da.Users.GetUser(_logon.Email);
                if ( user!=null ) {
                    if ( user.VerifyPassword(_password) ) {
                        logon(user);
                        result = true;
                    } else {
                        addError("password","Authentication failed");
                    }
                } else {
                    addError("email","No email found");
                }
            }

            return result;            
        }

        private void logon(User user) {
            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                    };

            var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
            _c.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal, new AuthenticationProperties()
            {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow + new TimeSpan(365, 0, 0, 0),
            }).Wait();
        }

        public bool ForgotPassword() {
            if ( string.IsNullOrEmpty(_logon.Email) ) {
                addError("email","Field is mandatory");
                return false;
            } else {
                var user = _da.Users.GetUser(_logon.Email);
                if ( user!=null ) {
                    sendChangePasswordLink(user);
                    return true;
                } else {
                    addError("email",$"No user registered with the given email address");
                    return false;
                }
            }
        }

        private void sendChangePasswordLink(User user) {
            // This link lasts for one day
            var ld = new LinkData<int>(user.Id, new TimeSpan(1, 0, 0, 0));
            string token = Crypto.Instance.EncryptAsBase64(ld.Serialize());
            string url = AppEnvironment.Instance.GetGuiUrl("/ResetPassword", new { token=token});
            //
            var email = new Email(Email.SystemEmailAddress.Admin);
            email.Send(user.Email,"Net Zero Energy Systems password reset",@$"
<div>Please find a link to reset your password below:-</div>
<br/>
<div>
<a href=""{url}"">{url}</a>
</div>
<br/>
<div>Net Zero Energy Systems</div>");
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

    public class ResetPassword {
        public string Token {get; set;}
        public string NewPassword1 {get; set;}
        public string NewPassword2 {get; set;}
        public LinkData<int> GetLinkData() {
            var token = Token.Replace(" ","+"); // + gets interpreted as space by the browser so need to revert
            string json;
            json = Crypto.Instance.DecryptFromBase64(token);
            var ld = JsonSerializer.Deserialize<LinkData<int>>(json);
            return ld;
        }
    }

    public class ResetPasswordModel : DbModel
    {
        private ResetPassword _resetPassword;
        private User _user;
        public ResetPasswordModel(ControllerBase c, ResetPassword resetPassword) : base(c) {
            _resetPassword = resetPassword;
        }

        protected override void checkModel()
        {
            //
            LinkData<int> ld;
            try {
                ld = _resetPassword.GetLinkData();
            } catch ( Exception) {
                this.addError("","Problem decrypting token - please request a new reset password link");
                return;
            }
            if ( ld.HasTimedOut()) {
                this.addError("","Link has timed out - please request a new reset password link");
            } else {
                _user = _da.Users.GetUser(ld.Data);
                //
                if ( _user==null)  {
                    this.addError("",$"Cannot find user with id=[{ld.Data}]");
                } else {
                    if ( string.IsNullOrEmpty(_resetPassword.NewPassword1) ) {
                        this.addError("newPassword1","Field is mandatory");
                    }
                    if ( string.IsNullOrEmpty(_resetPassword.NewPassword2) ) {
                        this.addError("newPassword2","Field is mandatory");
                    }
                    if ( !string.IsNullOrEmpty(_resetPassword.NewPassword1) && !string.IsNullOrEmpty(_resetPassword.NewPassword2) ) {
                        if ( _resetPassword.NewPassword1 != _resetPassword.NewPassword2 ) {
                            this.addError("newPassword2","Passwords do not match");
                        }
                    }
                } 
            }

        }

        protected override void beforeSave()
        {
            _user.SetPassword(_resetPassword.NewPassword1);
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
            if ( string.IsNullOrEmpty(_changePassword.Password) ) {
                this.addError("password","Field is mandatory");
            }
            if ( string.IsNullOrEmpty(_changePassword.NewPassword1) ) {
                this.addError("newPassword1","Field is mandatory");
            }
            if ( string.IsNullOrEmpty(_changePassword.NewPassword2) ) {
                this.addError("newPassword2","Field is mandatory");
            }
            //
            if ( this.Errors.Count>0 ) {
                return;
            }
            //
            if ( _user==null ) {
                throw new Exception("User not logged in");
            }
            if ( !_user.VerifyPassword(_changePassword.Password)) {
                this.addError("password","Authentication error");
            } else {
                if ( _changePassword.NewPassword1 != _changePassword.NewPassword2 ) {
                    this.addError("newPassword2","Passwords do not match");
                }
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