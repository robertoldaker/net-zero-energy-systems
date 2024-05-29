using System.Reflection;
using System.Text.Json;
using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;

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

    public IList<UserEdit> GetUserEdits(string tableName, int datasetId) {
            var data = Session.QueryOver<UserEdit>().
                Where(m=>m.Dataset.Id == datasetId).
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
            System.Linq.Expressions.Expression<Func<T,object>>[]? fetchFcns=null, 
            System.Linq.Expressions.Expression<Func<T,object>>? orderByFcn=null, 
            bool asc=true) 
            where T: class {
        //
        var datasetIds = GetAllDatasetIds(datasetId);
        //
        if ( datasetIds.Length==0) {
            throw new Exception($"No datasets found for datasetId=[{datasetId}]");
        }
        // look for objects using the root datasetId
        var q = Session.QueryOver<T>();
        if ( typeof(IDataset).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo())) {
            q = q.Where( m=>((IDataset) m).Dataset.Id == datasetIds[0]);
        }
        //
        if ( fetchFcns!=null) {
            foreach( var fFcn in fetchFcns) {
                q = q.Fetch(SelectMode.Fetch,fFcn);
            }
        }
        if ( orderByFcn!=null ) {
            if ( asc ) {
                q = q.OrderBy(orderByFcn).Asc;
            } else {
                q = q.OrderBy(orderByFcn).Desc;
            }
        } else if ( typeof(IId).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo()) ) {

            q = q.OrderBy(m=>((IId)m).Id).Asc;
        }
        // apply all user edits
        var data = q.List();
        applyUserEdits<T>(data,datasetIds, keyFcn);
        return data;
    }

    public IList<T> GetData<T>(
            int datasetId, 
            Func<T,string> keyFcn, 
            IQueryOver<T,T> queryOver) 
            where T: class {
        //
        var datasetIds = GetAllDatasetIds(datasetId);
        //
        if ( datasetIds.Length==0) {
            throw new Exception($"No datasets found for datasetId=[{datasetId}]");
        }
        // look for objects using the root datasetId
        var q = queryOver;
        if ( typeof(IDataset).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo())) {
            q = q.Where( m=>((IDataset) m).Dataset.Id == datasetIds[0]);
        }
        //
        if ( typeof(IId).GetTypeInfo().IsAssignableFrom(typeof(T).Ge‌​tTypeInfo()) ) {

            q = q.OrderBy(m=>((IId)m).Id).Asc;
        }
        // apply all user edits
        var data = q.List();
        applyUserEdits<T>(data,datasetIds, keyFcn);
        return data;
    }

    public void applyUserEdits<T>(IList<T> data,int[] datasetIds, Func<T,string> keyFcn) {
        // Gets dictionary of all user edits by version id
        var userEdits = GetUserEditsDict<T>(datasetIds);
        // Get properties of the base type
        var props = typeof(T).GetProperties();
        var propDict = new Dictionary<string,PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach( var prop in props) {
            propDict.Add(prop.Name.ToLower(),prop);
        }
        //
        foreach( var gp in data) {
            // key key to uniquely identifiy row
            var key = keyFcn(gp);
            // Loop over each version and apply each set of edits in order
            foreach( var vId in datasetIds) {
                // These are the edits to apply at this version
                var ues = userEdits[vId].Where(m=>m.Key == key);
                foreach ( var ue in ues) {
                    // See if the object has the property name based on the lower-case column name
                    if ( propDict.TryGetValue(ue.ColumnName, out PropertyInfo prop)) {
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
            }
        }
    }

    public void SaveUserEdit(UserEdit userEdit) {
        var ue = userEdit.Id!=0 ? Session.Get<UserEdit>(userEdit.Id) : null;
        Dataset dataset;
        if ( ue!=null ) {
            ue.Value = userEdit.Value;
            dataset = ue.Dataset;
        } else {
            dataset = GetDataset(userEdit.NewDatasetId);
            if ( dataset==null) {
                throw new Exception($"Could not find dataset for new useredit - datasetId = [{userEdit.NewDatasetId}]");
            } else {
                userEdit.Dataset = dataset;
                Session.Save(userEdit);
            }
        }
        // remove existing results since they have been invalidated
        // Delete all results that reference this version
        if ( dataset!=null ) {
            if ( dataset.Type == DatasetType.Elsi) {
                var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == dataset.Id).List();
                foreach( var er in ers) {
                    Session.Delete(er);
                }
            } else if ( dataset.Type == DatasetType.Loadflow ) {
                //??
                
            }

        }
    }

    public void DeleteUserEdit(int userEditId) {
        var ue = Session.Get<UserEdit>(userEditId);
        if ( ue!=null ) {
            // remove existing results since they have been invalidated
            // Delete all results that reference this version
            var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == ue.Dataset.Id).List();
            foreach( var er in ers) {
                Session.Delete(er);
            }
            Session.Delete(ue);
        }
    }

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
        }
        Session.Delete(dataset);
    }

    public Dataset GetDataset(int id) {
        return Session.Get<Dataset>(id);
    }

    public Dataset GetDataset(DatasetType type, string name, int userId, int id) {
        return Session.QueryOver<Dataset>().
            Where(m=>m.Name.IsLike(name)).
            And(m=>m.Type ==type).
            And(m=>m.Id!=id).
            And(m=>m.User.Id==userId).
            Take(1).SingleOrDefault();
    }

    public Dataset GetDataset(DatasetType type, string name, int userId) {
        return Session.QueryOver<Dataset>().
            Where(m=>m.Name.IsLike(name)).
            And(m=>m.Type ==type).
            And(m=>m.User.Id==userId).
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
            List();
    }
    public Dataset GetDataset( DatasetType type, int userId, string name) {
        return Session.QueryOver<Dataset>().
            Where( m=>m.User.Id == userId).
            And(m=>m.Type == type).
            And(m=>m.Name.IsLike(name)).
            Take(1).SingleOrDefault();
    }

    public int[] GetAllDatasetIds(int datasetId) {

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

    public DatasetData(DataAccess da, int versionId, 
            Func<T,string> keyFcn, 
            System.Linq.Expressions.Expression<Func<T,object>>[]? fetchFcns=null,
            System.Linq.Expressions.Expression<Func<T,object>>? orderByFcn=null, 
            bool asc=true) {
        Data = da.Datasets.GetData<T>(versionId, keyFcn, fetchFcns, orderByFcn, asc);
        TableName = typeof(T).Name;
        UserEdits = da.Datasets.GetUserEdits(TableName, versionId);
    }
    public DatasetData(DataAccess da, int datasetId, 
            Func<T,string> keyFcn, 
            IQueryOver<T,T> queryOver
        ) {
        Data = da.Datasets.GetData<T>(datasetId, keyFcn, queryOver);
        TableName = typeof(T).Name;
        UserEdits = da.Datasets.GetUserEdits(TableName, datasetId);
    }
    public DatasetData(IList<T> data, IList<UserEdit> userEdits) {
        Data = data;
        TableName = typeof(T).Name;
        UserEdits = userEdits;
    }
    public string TableName {get; private set;}
    public IList<T> Data {get; private set;}
    public IList<UserEdit> UserEdits{get; private set;}

}


        

 