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
                if ( _errors.Count==0 ) {
                    _da.CommitChanges();
                    afterSave();
                }
            }
            return _errors.Count==0;
        }

        protected virtual void checkModel() {

        }
        protected virtual void beforeSave() {

        }
        protected virtual void afterSave() {

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

    public class EditDatasetModel : DbModel
    {
        private Dataset _obj;
        public EditDatasetModel(ControllerBase c, Dataset obj) : base(c)
        {
            _obj = obj;
        }

        public EditDatasetModel(ControllerBase c, int id) : base(c)
        {
            _obj = _da.Datasets.GetDataset(id);
        }

        protected override void checkModel()
        {
            var userId = _c.GetUserId();
            if ( _da.Datasets.GetDataset(_obj.Type,_obj.Name,userId,_obj.Id)!=null) {
                this.addError("name","A dataset with this name already exists");
            }
        }

        protected override void beforeSave()
        {
            Dataset newObj;
            newObj = _da.Datasets.GetDataset(_obj.Id);
            newObj.Name = _obj.Name;
        }

        public void Delete() {
            // Don;t allow deleting of root object
            if (_obj!=null && _obj.Parent!=null ) {
                _da.Datasets.Delete(_obj);
                _da.CommitChanges();
            }
        }
    }

    public class NewDataset {
        public string Name {get; set;}
        public int ParentId {get; set;}
    }

    public class EditDataset {
        public string Name {get; set;}
        public int Id {get; set;}
        
        [ValidateNever]
        [JsonIgnore]
        public User user {get; set;}
    }

    public class NewDatasetModel : DbModel
    {
        private NewDataset _obj;
        public NewDatasetModel(ControllerBase c, NewDataset obj) : base(c)
        {
            _obj = obj;
        }

        protected override void checkModel()
        {
            var userId = _c.GetUserId();
            var parentObj = _da.Datasets.GetDataset(_obj.ParentId);
            if ( parentObj == null ) {
                this.addError("","Unexpected error - parent object is null");
            } else if (_da.Datasets.GetDataset(parentObj.Type,_obj.Name,userId,0)!=null) {
                this.addError("name","A dataset with this name already exists");
            }
        }

        protected override void beforeSave()
        {
            Dataset newObj;
            var user = _da.Users.GetUser(_c.GetUserId());
            newObj = new Dataset();
            var parentObj = _da.Datasets.GetDataset(_obj.ParentId);
            newObj.Parent = parentObj;
            newObj.User = user;
            _da.Datasets.Add(newObj);
            newObj.Name = _obj.Name;
            newObj.Type = parentObj.Type;
            Id = newObj.Id;
        }

        public int Id {get; private set;}
    }


}