using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Mapping;

namespace SmartEnergyLabDataApi.Data;

public class Datasets : DataSet
{
    public Datasets(DataAccess da) : base(da)
    {

    }

    public DataAccess DataAccess
    {
        get
        {
            return (DataAccess) _dataAccess;
        }
    }

    #region UserEdit
    public void Add(UserEdit userEdit) {
        Session.Save(userEdit);
    }

    public void Delete(UserEdit userEdit) {
        Session.Delete(userEdit);
    }

    public void AddDeleteUserEdit(IId obj, Dataset dataset) {
        //
        var tableName = obj.GetType().Name;
        var key = obj.Id.ToString();
        // Check one doesn;t already exist
        var ue = GetDeleteUserEdit(dataset.Id, tableName, key);        
        if ( ue==null ) {
            var userEdit = new UserEdit() {
                Dataset = dataset,
                TableName = tableName,
                Key = key,
                IsRowDelete = true
            };
            Add(userEdit);
        }
    }

    public UserEdit GetDeleteUserEdit(int datasetId, IId obj) {
        //
        var tableName = obj.GetType().Name;
        var key = obj.Id.ToString();
        //
        var data = Session.QueryOver<UserEdit>().
            Where(m=>m.Dataset.Id == datasetId).
            And(m=>m.TableName == tableName).
            And(m=>m.IsRowDelete).
            And(m=>m.Key == key).
            Take(1).SingleOrDefault();
        return data;
    }

    public UserEdit GetDeleteUserEdit(int datasetId, string tableName, string key) {
        var data = Session.QueryOver<UserEdit>().
            Where(m=>m.Dataset.Id == datasetId).
            And(m=>m.TableName == tableName).
            And(m=>m.IsRowDelete).
            And(m=>m.Key == key).
            Take(1).SingleOrDefault();
        return data;
    }

    public IList<UserEdit> GetUserEdits(string tableName, int datasetId) {
        var data = Session.QueryOver<UserEdit>().
            Where(m=>m.Dataset.Id == datasetId).
            And(m=>m.TableName == tableName).
            List();
        return data;
    }

    public IList<UserEdit> GetUserEdits(string tableName, string key) {
        var data = Session.QueryOver<UserEdit>().
            Where(m=>m.Key == key).
            And(m=>m.TableName == tableName).
            List();
        return data;
    }

    public Dictionary<int,List<UserEdit>> GetUserEditsDict<T>(int[] datasetIds) {
        var tableName = typeof(T).Name;
        var dict = new Dictionary<int,List<UserEdit>>();
        var all = Session.QueryOver<UserEdit>().
            Where(m=>m.Dataset.Id.IsIn(datasetIds)).
            And(m=>m.TableName == tableName).
            List();
        foreach( var id in datasetIds ) {
            var eds = all.Where(m=>m.Dataset.Id == id).ToList();
            dict.Add(id,eds);
        }
        return dict;
    }

    public IList<T> GetData<T>(
            int datasetId, 
            Func<T,string> keyFcn, 
            IQueryOver<T,T> queryOver,
            out List<UserEdit> userEdits,
            out List<T> deletedData) 
            where T: class {
        // this is the dataset plus the heirarchy going back to root
        var datasetIds = GetInheritedDatasetIds(datasetId);
        //
        if ( datasetIds.Length==0) {
            throw new Exception($"No datasets found for datasetId=[{datasetId}]");
        }
        // look for objects defined in datas
        var q = queryOver;
        if ( typeof(IDataset).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo())) {
            q = q.Where( m=>((IDataset) m).Dataset.Id.IsIn(datasetIds));
        }
        //
        if ( typeof(IId).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo()) ) {
            q = q.OrderBy(m=>((IId)m).Id).Asc;
        }
        // apply all user edits
        var data = q.List();
        applyUserEdits<T>(data,datasetIds, keyFcn, out userEdits, out deletedData);
        return data;
    }

    public void applyUserEdits<T>(IList<T> data,int[] datasetIds, Func<T,string> keyFcn, out List<UserEdit> userEdits, out List<T> deletedData) {
        // Gets dictionary of all user edits by version id
        var userEditsDict = GetUserEditsDict<T>(datasetIds);
        // Get properties of the base type
        var props = typeof(T).GetProperties();
        var propDict = new Dictionary<string,PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach( var prop in props) {
            propDict.Add(prop.Name.ToLower(),prop);
        }
        //
        int lastDatasetId = datasetIds.Last();
        userEdits = userEditsDict[lastDatasetId].Where(m=>!m.IsRowDelete).ToList();
        deletedData = new List<T>();
        //
        foreach( var gp in data) {
            // key key to uniquely identifiy row
            var key = keyFcn(gp);
            // Loop over each version and apply each set of edits in order
            foreach( var vId in datasetIds) {
                // These are the edits to apply at this version
                var ues = userEditsDict[vId].Where(m=>!m.IsRowDelete && m.Key == key);
                foreach ( var ue in ues) {
                    // See if the object has the property name based on the lower-case column name
                    if ( propDict.TryGetValue(ue.ColumnName, out PropertyInfo prop)) {
                        if ( vId == lastDatasetId ) {
                            ue.PrevValue = prop.GetValue(gp).ToString();
                        }
                        // if so apply change
                        if ( prop.PropertyType == typeof (double) || prop.PropertyType == typeof(double?) ) {
                            prop.SetValue(gp,ue.GetDoubleValue());
                        } else if ( prop.PropertyType == typeof (bool) || prop.PropertyType == typeof(bool?) ) {
                            prop.SetValue(gp,ue.GetBoolValue());
                        } else {
                            prop.SetValue(gp,ue.Value);
                        }
                    }
                }
                var ueds = userEditsDict[vId].Where(m=>m.IsRowDelete && m.Key == key);
                if ( ueds.Count()>0) {
                    deletedData.Add(gp);
                }
            }
        }

        // remove deletes from main list
        foreach( var dgp in deletedData) {
            data.Remove(dgp);
        }
    }

    #endregion

    #region Datasets
    public void Add(Dataset dataVersion) {
        Session.Save(dataVersion);
    }

    public void Delete(Dataset dataset) {
        
        // Delete children
        var children = Session.QueryOver<Dataset>().Where(m=>m.Parent.Id == dataset.Id).List();
        foreach( var dv in children) {
            Delete(dv);
        }
        // Delete all user edits that reference this version
        var ues = Session.QueryOver<UserEdit>().Where( m=>m.Dataset.Id == dataset.Id).List();
        foreach( var ue in ues) {
            Session.Delete(ue);
        }
        if ( dataset.Type == DatasetType.Elsi ) {
            // Delete all results that reference this version
            var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var er in ers) {
                Session.Delete(er);
            }
        } else if ( dataset.Type == DatasetType.Loadflow ) {
            // Branches
            var branches = Session.QueryOver<Branch>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var b in branches) {
                Session.Delete(b);
            }
            // Ctrls
            var ctrls = Session.QueryOver<Ctrl>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var c in ctrls) {
                Session.Delete(c);
            }
            // Nodes
            var nodes = Session.QueryOver<Node>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var n in nodes) {
                Session.Delete(n);
            }
            // BoundaryZones
            var bzs = Session.QueryOver<BoundaryZone>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var bz in bzs) {
                Session.Delete(bz);
            }
            // Boundaries
            var bs = Session.QueryOver<Boundary>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var b in bs) {
                Session.Delete(b);
            }
            // Zones
            var zones = Session.QueryOver<Zone>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var z in zones) {
                Session.Delete(z);
            }
            // Locations
            var locations = Session.QueryOver<GridSubstationLocation>().Where( m=>m.Dataset.Id == dataset.Id).List();
            foreach( var l in locations) {
                Session.Delete(l);
            }
        }
        Session.Delete(dataset);
    }

    public Dataset GetDataset(int id) {
        return Session.Get<Dataset>(id);
    }


    public Dataset GetDataset(DatasetType type, string name, int userId, int id) {
        return Session.QueryOver<Dataset>().
            Where(m=>m.Name.IsInsensitiveLike(name)).
            And(m=>m.Type ==type).
            And(m=>m.Id!=id).
            And(m=>m.User.Id==userId || m.User==null).
            Take(1).SingleOrDefault();
    }

    public Dataset GetDataset(DatasetType type, string name) {
        return Session.QueryOver<Dataset>().
            Where(m=>m.Name.IsLike(name)).
            And(m=>m.Type ==type).
            And(m=>m.User==null).
            Take(1).SingleOrDefault();
    }

    public Dataset GetRootDataset(DatasetType type) {
        return Session.QueryOver<Dataset>().
            And(m=>m.Type ==type).
            And(m=>m.User==null).
            And(m=>m.Parent==null).
            Take(1).SingleOrDefault();
    }

    public IList<Dataset> GetDatasets( DatasetType type, int userId) {

        return Session.QueryOver<Dataset>().
            Where( m=>(m.User.Id == userId) || m.User==null).
            And(m=>m.Type == type).
            OrderBy(m=>m.Name).Asc.
            List();
    }
    public Dataset GetDataset( DatasetType type, int userId, string name) {
        return Session.QueryOver<Dataset>().
            Where( m=>m.User.Id == userId).
            And(m=>m.Type == type).
            And(m=>m.Name.IsLike(name)).
            Take(1).SingleOrDefault();
    }

    public int[] GetInheritedDatasetIds(int datasetId) {

        var q = Session.QueryOver<Dataset>().
            Where(m=>m.Id == datasetId).
            Fetch(SelectMode.Fetch,m=>m.Parent);
        var ds = q.Take(1).SingleOrDefault();

        var dss = new List<int>();
        //
        if ( ds.Parent == null ) {
            // root dataset is a special case
            dss.Add(ds.Id);
        } else {
            while (ds!=null && ds.Parent!=null) {
                dss.Add(ds.Id);
                ds = ds.Parent;
            }
            // Reverse so we have the lowest dataset in the heirarchy first and then incremental change up the
            // the version we are interested in
            dss.Reverse();
        }
        return dss.ToArray();
    }

    /// <summary>
    /// Gets array of all dataset ids that derived from the dataset provided including the dataset itself.
    /// </summary>
    /// <param name="datasetId"></param>
    /// <returns></returns> <summary>
    public int[] GetDerivedDatasetIds(int datasetId) {
        var dsIds = new List<int>();
        addDerivedDatasetIds(datasetId,dsIds);
        return dsIds.ToArray();
    }

    private void addDerivedDatasetIds(int datasetId, List<int> dsIds) {
        dsIds.Add(datasetId);
        var q = Session.QueryOver<Dataset>().
            Where(m=>m.Parent.Id == datasetId).Select(m=>m.Id);
        var children = q.List<int>();
        foreach( var dsId in children) {
            addDerivedDatasetIds(dsId,dsIds);
        }
    }

    #endregion
}

public interface IDataset 
{
    public Dataset Dataset {get; set;}
}

public interface IId
{
    public int Id {get;set;}
}

public class DatasetData<T> where T : class {

    public DatasetData(DataAccess da, int datasetId, 
            Func<T,string> keyFcn, 
            IQueryOver<T,T> queryOver
        ) {
        TableName = typeof(T).Name;
        Data = da.Datasets.GetData<T>(datasetId, keyFcn, queryOver, out var userEdits, out var deletedData);
        UserEdits = userEdits;
        DeletedData = deletedData;
    }
    public string TableName {get; private set;}
    public IList<T> Data {get; private set;}
    public IList<T> DeletedData {get; private set;}
    public IList<UserEdit> UserEdits{get; private set;}

}


        

 