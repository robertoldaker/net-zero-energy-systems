using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Dialect.Function;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq.Functions;
using SmartEnergyLabDataApi.Controllers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data;

public class EditItem {
    public int id { get; set; }
    public string className { get; set; }
    public int datasetId { get; set; }
    public Dictionary<string, object>? data { get; set; }
}

public class DeletedItem {
    public DeletedItem(IDatasetIId obj)
    {
        id = obj.Id;
        className = obj.GetType().Name;
        datasetId = obj.Dataset.Id;
    }
    public int id { get; set; }
    public string className { get; set; }
    public int datasetId { get; set; }
    public bool isSourceDelete { get; set; }
}

public interface IEditItemHandler {

    IDatasetIId GetItem(EditItemModel m);

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

    public abstract IDatasetIId GetItem(EditItemModel m);

    public abstract void Save(EditItemModel m);
}

public class EditItemModel : DbModel {
    private EditItem _editItem;
    private Dataset _dataset;
    private IEditItemHandler _handler;
    private IDatasetIId _item;
    private List<DeletedItem> _deletedItems = new List<DeletedItem>();

    private List<DatasetData<object>> _datasetData;

    private static Dictionary<string, IEditItemHandler> _handlerDict = new Dictionary<string, IEditItemHandler>();

    public static void AddHandler<T>(IEditItemHandler handler)
    {
        lock (_handlerDict) {
            var className = typeof(T).Name;
            if (_handlerDict.ContainsKey(className)) {
                throw new Exception($"EditItemModel already has a handler for className=[{className}]");
            }
            _handlerDict.Add(className, handler);
        }
    }

    private IEditItemHandler getHandler(string className)
    {
        lock (_handlerDict) {
            if (_handlerDict.ContainsKey(className)) {
                return _handlerDict[className];
            } else {
                throw new Exception($"No handler defined for className=[{className}]");
            }
        }
    }

    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() {
        PropertyNameCaseInsensitive = true
    };

    public EditItemModel(ControllerBase c, EditItem editItem) : base(c)
    {
        //
        _editItem = editItem;
        _dataset = _da.Datasets.GetDataset(editItem.datasetId);
        if (_dataset == null) {
            throw new Exception($"Cannot find dataset with id=[{editItem.datasetId}]");
        }
        //
        checkAuthorisation();
        //
        _handler = getHandler(_editItem.className);
        //
        _item = _handler.GetItem(this);
    }

    private void checkAuthorisation()
    {
        var userId = _c.GetUserId();
        if (!_c.IsAuthenticated() || userId == 0) {
            throw new Exception("Not authorised");
        } else {
            var user = _da.Users.GetUser(userId);
            if (user != null) {
                if (_dataset.User != null && _dataset.User.Id != user.Id) {
                    throw new Exception($"Not authorised to edit dataset owned by different user, dataset owner id=[{_dataset.User.Id}], user id=[{user.Id}]");
                } else if (_dataset.User == null && user.Role != UserRole.Admin) {
                    throw new Exception("Not authorised to edit system owned datasets");
                }
            } else {
                throw new Exception($"Unexpected null user for userId=[{userId}]");
            }
        }
    }

    public DataAccess Da
    {
        get {
            return _da;
        }
    }

    public int ItemId
    {
        get {
            return _editItem.id;
        }
    }

    public Dataset Dataset
    {
        get {
            return _dataset;
        }
    }

    public IId Item
    {
        get {
            return _item;
        }
    }

    public EditItem EditItem
    {
        get {
            return _editItem;
        }
    }

    public List<DatasetData<object>> DatasetData
    {
        get {
            return _datasetData;
        }
    }

    public List<DeletedItem> DeletedItems
    {
        get {
            return _deletedItems;
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
        if (IsSourceEdit()) {
            _handler.Save(this);
        } else {
            _handler.UpdateUserEdits(this);
        }
        // Delete all existing results for this dataset and any derived ones
        if (_dataset.Type == DatasetType.Elsi) {
            _da.Elsi.DeleteResults(_dataset.Id);
        } else if (_dataset.Type == DatasetType.BoundCalc) {
            _da.BoundCalc.DeleteResults(_dataset.Id);
        } else {
            throw new Exception($"Unexpected dataset type [{_dataset.Type}]");
        }
    }

    public void UpdateItemUserEdit()
    {
        UpdateUserEditForItem(_item);
    }

    public void UpdateUserEditForItem(IId item)
    {
        //
        var className = item.GetType().Name;

        // get all user edits in the inheritence heirarchy
        var allDatasetIds = _da.Datasets.GetInheritedDatasetIds(_dataset.Id);
        var userEditDict = _da.Datasets.GetUserEditsDict(className, ((IId)item).Id.ToString(), allDatasetIds);

        // thse are the current user edits for this dataset
        List<UserEdit> userEdits;
        if (userEditDict.ContainsKey(_dataset.Id)) {
            userEdits = userEditDict[_dataset.Id];
            // remove it from the dictionary so the dictionary contains just inherited dataset ids
            userEditDict.Remove(_dataset.Id);
        } else {
            userEdits = new List<UserEdit>();
        }
        // these are the inheritedDatasetIds without the current one in heirarchical order
        var inheritedDatasetIds = userEditDict.Keys.OrderBy(m => m).ToArray();

        // Get properties and propertInfo as a dictionary
        var props = item.GetType().GetProperties();
        var propDict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in props) {
            propDict.Add(prop.Name.ToLower(), prop);
        }

        // Loop over each item in the data array and set values for each
        foreach (var name in _editItem.data.Keys) {
            // Check there is a property with the given name
            if (propDict.TryGetValue(name, out PropertyInfo prop)) {
                // get the item value
                var itemValue = prop.GetValue(item);
                // apply user edits so we get the latest value
                var itemStrValue = applyUserEdits(name, objToString(itemValue), userEditDict, inheritedDatasetIds);
                // get the new value
                var editValue = _editItem.data[name];
                // get any existing user edit for this
                var userEdit = userEdits.Where(m => string.Compare(m.ColumnName, name, true) == 0).FirstOrDefault();
                if (itemStrValue == objToString(editValue)) {
                    // No change so delete
                    if (userEdit != null) {
                        _da.Datasets.Delete(userEdit);
                    }
                } else {
                    // No existing userEdit so create new one
                    if (userEdit == null) {
                        string key = ((IId)item).Id.ToString();
                        userEdit = new UserEdit() { TableName = className, ColumnName = prop.Name, Dataset = _dataset, Key = key };
                        _da.Datasets.Add(userEdit);
                    }
                    // set value of user edit
                    userEdit.Value = objToString(editValue);
                }
            }
        }
        //
    }

    private string applyUserEdits(string name, string initialValue, Dictionary<int, List<UserEdit>> userEditDict, int[] datasetIds)
    {
        string newValue = initialValue;
        foreach (var datasetId in datasetIds) {
            if (userEditDict.ContainsKey(datasetId)) {
                var userEdits = userEditDict[datasetId];
                var ue = userEdits.Where(m => string.Compare(m.ColumnName, name, true) == 0).FirstOrDefault();
                if (ue != null) {
                    newValue = ue.Value;
                }
            }
        }
        return newValue;
    }



    private string objToString(object? obj)
    {
        return obj != null ? obj.ToString() : "";
    }

    public bool GetString(string name, out string value)
    {
        if (_editItem.data.TryGetValue(name, out object valueObj)) {
            value = valueObj != null ? valueObj.ToString() : "";
            return true;
        } else {
            value = null;
            return false;
        }
    }

    public bool HasDataField(string name)
    {
        return _editItem.data.ContainsKey(name);
    }

    public bool? CheckBoolean(string name)
    {
        if (GetString(name, out string boolStr)) {
            if (boolStr != null && bool.TryParse(boolStr, out bool value)) {
                return value;
            } else {
                throw new Exception($"Cannot parse bool value [{boolStr}]");
            }
        } else {
            return null;
        }
    }

    public int? CheckInt(string name)
    {
        if (GetString(name, out string intStr)) {
            if (intStr != null && int.TryParse(intStr, out int value)) {
                return value;
            } else {
                throw new Exception($"Cannot parse int value [{intStr}]");
            }
        } else {
            return null;
        }
    }

    public T? CheckEnum<T>(string name) where T : struct, System.Enum
    {
        if (GetString(name, out string strValue)) {
            if (int.TryParse(strValue, out int value)) {
                if (!Enum.IsDefined(typeof(T), value)) {
                    this.addError(name, $"Unexpected problem parsing value [{value}] for field [{name}]");
                    return default;
                }
                return (T)Enum.ToObject(typeof(T), value);
            } else if ( Enum.TryParse<T>(strValue, out T enumVal) ) {
                return enumVal;
            } else {
                this.addError(name, $"Needs to be set to something");
                return null;
            }
        } else {
            return null;
        }
    }

    public double? CheckDouble(string name, double? low = null, double? high = null)
    {
        if (GetString(name, out string valueStr)) {
            if (double.TryParse(valueStr, out double value)) {
                if (low != null && value < low) {
                    this.addError(name, $"Must be >= {low}");
                }
                if (high != null && value > high) {
                    this.addError(name, $"Must be <= {high}");
                }
            } else {
                this.addError(name, $"Needs to be a number");
            }
            return value;
        } else {
            return null;
        }
    }

    public int[]? GetIntArray(string name)
    {
        if (_editItem.data.TryGetValue(name, out object jsonObj)) {
            return ((JsonElement)jsonObj).Deserialize<int[]>();
        } else {
            return null;
        }
    }

    public void AddError(string name, string message)
    {
        addError(name, message);
    }

    public string Delete()
    {
        var isSourceEdit = IsSourceEdit();
        var msg = _handler.BeforeDelete(this, isSourceEdit);
        if (string.IsNullOrEmpty(msg)) {
            DeleteObject(_item);
            _da.CommitChanges();
        }
        if (string.IsNullOrEmpty(msg)) {
            _datasetData = _handler.GetDatasetData(this);
        }
        return msg;
    }

    public string UnDelete()
    {
        var msg = _handler.BeforeUndelete(this);
        if (string.IsNullOrEmpty(msg)) {
            var ue = _da.Datasets.GetDeleteUserEdit(_dataset.Id, _editItem.className, _editItem.id.ToString());
            if (ue != null) {
                _da.Datasets.Delete(ue);
                _da.CommitChanges();
            }
        }
        _datasetData = _handler.GetDatasetData(this);
        return msg;
    }

    public bool IsSourceEdit()
    {
        IDataset dItem = (IDataset)_item;
        return _dataset.Id == dItem.Dataset.Id;
    }

    public bool UnDeleteObject<T>(T obj) where T : IId
    {
        return Da.Datasets.UnDelete<T>(_dataset.Id, obj.Id.ToString());
    }

    public bool UnDeleteObject<T>(int id)
    {
        return Da.Datasets.UnDelete<T>(_dataset.Id, id.ToString());
    }
    public DeletedItem DeleteObject(IDatasetIId obj, bool saveToList=true)
    {
        var item = new DeletedItem(obj);
        item.isSourceDelete = Da.Datasets.DeleteObject(obj, _dataset);
        if (item.isSourceDelete && saveToList) {
            _deletedItems.Add(item);
        }
        return item;
    }
}
