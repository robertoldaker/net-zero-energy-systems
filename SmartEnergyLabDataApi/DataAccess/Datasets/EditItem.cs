using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Dialect.Function;
using NHibernate.Hql.Ast.ANTLR.Tree;
using SmartEnergyLabDataApi.Controllers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data;

public class EditItem {
    public int id {get; set;}
    public string className {get; set;}
    public int datasetId {get; set;}
    public Dictionary<string,object>? data {get; set;}

}

public interface IEditItemHandler {

    IId GetItem(EditItemModel m);

    void Check(EditItemModel m);

    void Save(EditItemModel m);

    string BeforeUndelete(EditItemModel m);

    string BeforeDelete(EditItemModel m, bool isSourceEdit);

    void UpdateUserEdits(EditItemModel m);

    List<DatasetData<object>> GetDatasetData(EditItemModel m);
}

public abstract class BaseEditItemHandler : IEditItemHandler
{
    public virtual string BeforeDelete(EditItemModel m, bool isSourceEdit)
    {
        return "";
    }

    public virtual string BeforeUndelete(EditItemModel m)
    {
        return "";
    }

    public virtual void UpdateUserEdits(EditItemModel m) {
        m.UpdateItemUserEdit();
    }

    public abstract void Check(EditItemModel m);

    public abstract List<DatasetData<object>> GetDatasetData(EditItemModel m);

    public abstract IId GetItem(EditItemModel m);

    public abstract void Save(EditItemModel m);
}

public class EditItemModel : DbModel {
    private EditItem _editItem;
    private Dataset _dataset;
    private IEditItemHandler _handler;
    private IId _item;

    private List<DatasetData<object>> _datasetData;

    private static Dictionary<string,IEditItemHandler> _handlerDict = new Dictionary<string, IEditItemHandler>();

    public static void AddHandler<T>(IEditItemHandler handler) {
        lock( _handlerDict) {
            var className = typeof(T).Name;
            if ( _handlerDict.ContainsKey(className) ) {
                throw new Exception($"EditItemModel already has a handler for className=[{className}]");
            }
            _handlerDict.Add(className,handler);
        }
    }

    private IEditItemHandler getHandler(string className) {
        lock( _handlerDict) {
            if ( _handlerDict.ContainsKey(className)) {
                return _handlerDict[className];
            } else {
                throw new Exception($"No handler defined for className=[{className}]");
            }
        }
    }

    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() {
                 PropertyNameCaseInsensitive = true
            };

    public EditItemModel(ControllerBase c, EditItem editItem) : base(c) {
        //
        _editItem = editItem;
        _dataset = _da.Datasets.GetDataset(editItem.datasetId);
        if ( _dataset==null ) {
            throw new Exception($"Cannot find dataset with id=[{editItem.datasetId}]");
        }
        //
        checkAuthorisation();
        //
        _handler = getHandler(_editItem.className);
        //
        _item = _handler.GetItem(this);
    }

    private void checkAuthorisation() {
        var userId = _c.GetUserId();
        if ( !_c.IsAuthenticated() || userId ==0 ) {
            throw new Exception("Not authorised");
        } else {
            var user = _da.Users.GetUser(userId);
            if ( user!=null ) {
                if ( _dataset.User!=null && _dataset.User.Id!=user.Id ) {
                    throw new Exception($"Not authorised to edit dataset owned by different user, dataset owner id=[{_dataset.User.Id}], user id=[{user.Id}]");
                } else if ( _dataset.User==null && user.Role!=UserRole.Admin) {
                    throw new Exception("Not authorised to edit system owned datasets");
                }
            } else {
                throw new Exception($"Unexpected null user for userId=[{userId}]");
            }
        }
    }

    public DataAccess Da {
        get {
            return _da;
        }
    }

    public int ItemId {
        get {
            return _editItem.id;
        }
    }

    public Dataset Dataset {
        get {
            return _dataset;
        }
    }

    public IId Item {
        get {
            return _item;
        }
    }

    public List<DatasetData<object>> DatasetData {
        get {
            return _datasetData;
        }
    }

    protected override void afterSave()
    {
        base.afterSave();
        //
        _datasetData = _handler.GetDatasetData(this);
    }

    protected override void checkModel()
    {
        base.checkModel();
        //
        _handler.Check(this);
    }

    protected override void beforeSave()
    {
        base.beforeSave();
        if ( IsSourceEdit() ) {
            _handler.Save(this);
        } else {
            _handler.UpdateUserEdits(this);
        }
        // Delete all existing results for this dataset and any derived ones
        if ( _dataset.Type == DatasetType.Elsi) {
            _da.Elsi.DeleteResults(_dataset.Id);
        } else if ( _dataset.Type == DatasetType.BoundCalc) {
            _da.BoundCalc.DeleteResults(_dataset.Id);
        } else {
            throw new Exception($"Unexpected dataset type [{_dataset.Type}]");
        }
    }

    public void UpdateItemUserEdit() {
        UpdateUserEditForItem(_item);
    }

    public void UpdateUserEditForItem(IId item) {
        var className = item.GetType().Name;
        var userEdits = _da.Datasets.GetUserEdits(className,_dataset.Id);
        var props = item.GetType().GetProperties();
        var propDict = new Dictionary<string,PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach( var prop in props) {
            propDict.Add(prop.Name.ToLower(),prop);
        }
        //
        foreach( var name in _editItem.data.Keys) {
            if ( propDict.TryGetValue(name, out PropertyInfo prop)) {
                // 
                var itemValue = prop.GetValue(item);
                var editValue = _editItem.data[name]; 
                string key = ((IId) item).Id.ToString();
                var userEdit = userEdits.Where( m=>string.Compare(m.ColumnName,name,true)==0 && m.Key == key).FirstOrDefault();
                if ( objToString(itemValue) == objToString(editValue)) {
                    //
                    if ( userEdit!=null ) {
                        _da.Datasets.Delete(userEdit);
                    }
                } else {
                    if ( userEdit==null) {
                        userEdit = new UserEdit() { TableName = className, ColumnName=prop.Name,Dataset = _dataset, Key = key  };
                        _da.Datasets.Add(userEdit);
                    }
                    userEdit.Value = objToString(editValue);
                }
            }
        }
        //

    }

    private string objToString(object? obj) {
        return obj!=null ? obj.ToString() : "";
    }

    public bool GetString(string name, out string value) {
        if ( _editItem.data.TryGetValue(name,out object valueObj)) {
            value = valueObj.ToString();
            return true;
        } else {
            value = null;
            return false;
        }
    }

    public bool? CheckBoolean(string name) {
        if ( GetString(name, out string boolStr)) {
            if ( bool.TryParse(boolStr, out bool value) ) {
                return value;
            } else {
                throw new Exception($"Cannot parse bool value [{boolStr}]");
            }
        } else {
            return null;
        }
    }

    public int? CheckInt(string name) {
        if ( GetString(name, out string intStr)) {
            if ( int.TryParse(intStr, out int value) ) {
                return value;
            } else {
                throw new Exception($"Cannot parse int value [{intStr}]");
            }
        } else {
            return null;
        }
    }

    public double? CheckDouble(string name, double? low=null, double? high=null) {
        if ( GetString(name, out string valueStr)) {
            if ( double.TryParse(valueStr, out double value)) {
                if ( low!=null && value<low) {
                    this.addError(name,$"Must be >= {low}");
                }
                if ( high!=null && value>high) {
                    this.addError(name,$"Must be <= {high}");
                }
            } else {
                this.addError(name,$"Needs to be a number");
            }
            return value;
        } else {
            return null;
        }
    }

    public int[]? GetIntArray(string name) {
        if ( _editItem.data.TryGetValue(name,out object jsonObj)) {
            return ((JsonElement) jsonObj).Deserialize<int[]>();
        } else {
            return null;
        }
    }

    public void AddError(string name, string message) {
        addError(name, message);
    }

    public string Delete() {
        var isSourceEdit = IsSourceEdit();
        var msg = _handler.BeforeDelete(this, isSourceEdit);
        if ( string.IsNullOrEmpty(msg) ) {
            if ( isSourceEdit ) {
                // remove item
                _da.Session.Delete(_item);
                // remove all user edits associated with object
                var userEdits = _da.Datasets.GetUserEdits(_editItem.className,((IId) _item).Id.ToString());
                foreach( var ue in userEdits) {
                    _da.Datasets.Delete(ue);
                }
            } else {
                _da.Datasets.AddDeleteUserEdit((IId) _item,_dataset);
            }       
            _da.CommitChanges();
        }
        return msg;
    }

    public string UnDelete() {
        var msg = _handler.BeforeUndelete(this);
        if ( string.IsNullOrEmpty(msg)) {
            var ue = _da.Datasets.GetDeleteUserEdit(_dataset.Id, _editItem.className, _editItem.id.ToString());
            if ( ue!=null ) {
                _da.Datasets.Delete(ue);
                _da.CommitChanges();
            }
        }
        _datasetData = _handler.GetDatasetData(this);
        return msg;
    }

    private bool IsSourceEdit() {
        IDataset dItem = (IDataset) _item;
        return _dataset.Id == dItem.Dataset.Id;
    }
}