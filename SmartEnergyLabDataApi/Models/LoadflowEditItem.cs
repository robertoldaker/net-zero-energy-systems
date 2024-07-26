using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Dialect.Function;
using NHibernate.Hql.Ast.ANTLR.Tree;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

public class LoadflowEditItem {
    public int id {get; set;}
    public string className {get; set;}
    public int datasetId {get; set;}
    public Dictionary<string,object>? data {get; set;}

}

public class LoadflowEditItemModel : DbModel {
    private LoadflowEditItem _editItem;
    private Dataset _dataset;
    private object _item;

    private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() {
                 PropertyNameCaseInsensitive = true
            };
    public LoadflowEditItemModel(ControllerBase c, LoadflowEditItem editItem) : base(c) {
        _editItem = editItem;
        _dataset = _da.Datasets.GetDataset(editItem.datasetId);
        if ( _dataset==null ) {
            throw new Exception($"Cannot find dataset with id=[{editItem.datasetId}]");
        }
        setItem();
    }

    private void setItem() {
        var className = _editItem.className;
        var id = _editItem.id;
        if ( className == "Node") {
            _item = id>0 ? _da.Loadflow.GetNode(id) : new Node(_dataset);
        } else {
            throw new Exception($"Unexpected valus of className [{className}]");
        }
        if ( _item==null) {
            throw new Exception($"Could not find item with className [{className}] and id [{id}]");
        }
    }

    protected override void checkModel()
    {
        base.checkModel();
        //
        if ( _item is Node) {
            checkNode();
        } else {
            throw new Exception($"Unexpected type of item [{_item.GetType().Name}]");
        }
    }

    protected override void beforeSave()
    {
        base.beforeSave();
        if ( IsSourceEdit() ) {
            if ( _item is Node) {
                saveNode();
            }
        } else {
            updateUserEdit();
        }
    }

    private void updateUserEdit() {
        var userEdits = _da.Datasets.GetUserEdits(_editItem.className,_dataset.Id);
        var props = _item.GetType().GetProperties();
        var propDict = new Dictionary<string,PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach( var prop in props) {
            propDict.Add(prop.Name.ToLower(),prop);
        }
        //
        foreach( var name in _editItem.data.Keys) {
            if ( propDict.TryGetValue(name, out PropertyInfo prop)) {
                // 
                var itemValue = prop.GetValue(_item);
                var editValue = _editItem.data[name]; 
                var userEdit = userEdits.Where( m=>string.Compare(m.ColumnName,name,true)==0 ).FirstOrDefault();
                if ( itemValue.ToString() == editValue.ToString()) {
                    //
                    if ( userEdit!=null ) {
                        _da.Datasets.Delete(userEdit);
                    }
                } else {
                    if ( userEdit==null) {
                        userEdit = new UserEdit() { TableName = _item.GetType().Name, ColumnName=prop.Name,Dataset = _dataset, Key = ((IId) _item).Id.ToString()  };
                        _da.Datasets.Add(userEdit);
                    }
                    userEdit.Value = editValue.ToString();
                }
            }
        }
        //

    }

    private void checkNode() {
        // code
        if ( getString("code",out string code)) {
            Regex regex = new Regex(@"^[A-Z]{4}\d");
            var codeMatch = regex.Match(code);        
            if ( !codeMatch.Success) {
                this.addError("code","Code must be in form <uppercase-4-letter-code><voltage id><anything>");
            } 
        }
        // demand        
        checkDouble("demand",0);
        // generation
        checkDouble("generation",0);
        // external
        checkBoolean("ext");
        // zone id
        checkInt("zoneId");
    }

    private bool getString(string name, out string value) {
        if ( _editItem.data.TryGetValue(name,out object valueObj)) {
            value = valueObj.ToString();
            return true;
        } else {
            value = null;
            return false;
        }
    }

    private bool? checkBoolean(string name) {
        if ( getString(name, out string boolStr)) {
            if ( bool.TryParse(boolStr, out bool value) ) {
                return value;
            } else {
                throw new Exception($"Cannot parse bool value [{boolStr}]");
            }
        } else {
            return null;
        }
    }

    private int? checkInt(string name) {
        if ( getString(name, out string intStr)) {
            if ( int.TryParse(intStr, out int value) ) {
                return value;
            } else {
                throw new Exception($"Cannot parse int value [{intStr}]");
            }
        } else {
            return null;
        }
    }

    private void saveNode() {
        //
        Node node = (Node) _item;
        //
        if ( getString("code",out string code)) {
            node.Code = code;
        }
        // demand        
        var demand = checkDouble("demand",0);
        if ( demand!=null ) {
            node.Demand = (double) demand;
        }
        // generation
        var generation = checkDouble("generation",0);
        if ( generation!=null ) {
            node.Generation = (double) generation;            
        }
        // external
        var ext = checkBoolean("ext");
        if ( ext!=null) {
            node.Ext = (bool) ext;
        }        
        // zone id
        var zoneId = checkInt("zoneId");
        if ( zoneId!=null ) {
            var zone = _da.Loadflow.GetZone((int) zoneId);
            node.Zone = zone;
        } 

        //
        if ( node.Id==0) {
            _da.Loadflow.Add(node);
        }
    }

    private double? checkDouble(string name, double? low=null, double? high=null) {
        if ( getString(name, out string valueStr)) {
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

    public void Delete() {
        if ( IsSourceEdit() ) {
            if ( _item is Node ) {
                _da.Loadflow.Delete((Node) _item);
            }
            // remove all user edits associated with object
            var userEdits = _da.Datasets.GetUserEdits(_editItem.className,((IId) _item).Id.ToString());
            foreach( var ue in userEdits) {
                _da.Datasets.Delete(ue);
            }
        } else {
            // marks it as being deleted
            var userEdit = new UserEdit() {
                Dataset = _dataset,
                TableName = _editItem.className,
                Key = _editItem.id.ToString(),
                IsRowDelete = true
            };
            _da.Datasets.Add(userEdit);
        }       
        _da.CommitChanges();
    }

    public void UnDelete() {

        var ue = _da.Datasets.GetDeleteUserEdit(_dataset.Id, _editItem.className, _editItem.id.ToString());
        if ( ue!=null ) {
            _da.Datasets.Delete(ue);
            _da.CommitChanges();
        }
    }

    private bool IsSourceEdit() {
        IDataset dItem = (IDataset) _item;
        return _dataset.Id == dItem.Dataset.Id;
    }

}