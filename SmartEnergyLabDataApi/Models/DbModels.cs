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
    public abstract class DbModel : IDisposable {
        protected DataAccess _da;
        protected ControllerBase _c;
        private Dictionary<string,string> _errors;

        public DbModel(ControllerBase c) {
            _c = c;
            _da = new DataAccess();
            _errors = new Dictionary<string, string>();
        }

        public void Dispose()
        {
            _da.Dispose();
        }

        public bool Save() {
            checkModel();
            if ( _errors.Count==0 ) {
                beforeSave();
                _da.CommitChanges();
            }
            return _errors.Count==0;
        }

        protected virtual void checkModel() {

        }
        protected virtual void beforeSave() {

        }

        public Dictionary<string,string> Errors {
            get {
                return _errors;
            }
        }

        protected void addError(string name, string message) {
            _errors.Add(name,message);
        }
    }

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

    public class EditElsiDataVersionModel : DbModel
    {
        private ElsiDataVersion _obj;
        public EditElsiDataVersionModel(ControllerBase c, ElsiDataVersion obj) : base(c)
        {
            _obj = obj;
        }

        public EditElsiDataVersionModel(ControllerBase c, int id) : base(c)
        {
            _obj = _da.Elsi.GetDataVersion(id);
        }

        protected override void checkModel()
        {
            var userId = _c.GetUserId();
            if ( _da.Elsi.GetDataVersion(_obj.Name,userId,_obj.Id)!=null) {
                this.addError("name","A dataset with this version already exists");
            }
        }

        protected override void beforeSave()
        {
            ElsiDataVersion newObj;
            newObj = _da.Elsi.GetDataVersion(_obj.Id);
            newObj.Name = _obj.Name;
        }

        public void Delete() {
            // Don;t allow deleting of root object
            if (_obj!=null && _obj.Parent!=null ) {
                _da.Elsi.Delete(_obj);
                _da.CommitChanges();
            }
        }
    }

    public class NewElsiDataVersion {
        public string Name {get; set;}
        public int ParentId {get; set;}
    }

    public class EditElsiDataVersion {
        public string Name {get; set;}
        public int Id {get; set;}
        
        [ValidateNever]
        [JsonIgnore]
        public User user {get; set;}
    }

    public class NewElsiDataVersionModel : DbModel
    {
        private NewElsiDataVersion _obj;
        public NewElsiDataVersionModel(ControllerBase c, NewElsiDataVersion obj) : base(c)
        {
            _obj = obj;
        }

        protected override void checkModel()
        {
            var userId = _c.GetUserId();
            if ( _da.Elsi.GetDataVersion(_obj.Name,userId,0)!=null) {
                this.addError("name","A dataset with this name already exists");
            }
        }

        protected override void beforeSave()
        {
            ElsiDataVersion newObj;
            var user = _da.Users.GetUser(_c.GetUserId());
            newObj = new ElsiDataVersion();
            var parentObj = _da.Elsi.GetDataVersion(_obj.ParentId);
            newObj.Parent = parentObj;
            newObj.User = user;
            _da.Elsi.Add(newObj);
            newObj.Name = _obj.Name;
            Id = newObj.Id;
        }

        public int Id {get; private set;}


    }
}