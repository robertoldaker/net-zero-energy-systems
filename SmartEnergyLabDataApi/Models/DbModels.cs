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