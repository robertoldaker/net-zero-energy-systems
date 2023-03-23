using System.Reflection;
using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Elsi;

namespace SmartEnergyLabDataApi.Data
{
    public class Elsi : DataSet
    {
        public Elsi(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        #region GenParameter
        public void Add(GenParameter obj) {
            Session.Save(obj);
        }

        public void Delete(GenParameter obj) {
            Session.Delete(obj);
        }

        public IList<GenParameter> GetGenParameters() {
            return Session.QueryOver<GenParameter>().List();
        }

        public IList<GenParameter> GetGenParameters(int versionId) {
            //
            var data = Session.QueryOver<GenParameter>().List();
            // apply all user edits
            applyUserEdits<GenParameter>(data, versionId, (m)=>m.Type.ToString());
            return data;
        }

        public IList<T> GetData<T>(int versionId, Func<T,string> keyFcn) where T: class{
            //
            var data = Session.QueryOver<T>().List();
            // apply all user edits
            applyUserEdits<T>(data, versionId, keyFcn);
            return data;
        }

        #endregion

        #region GenCapacity
        public void Add(GenCapacity cap) {
            Session.Save(cap);
        }

        public void Delete(GenCapacity cap) {
            Session.Delete(cap);
        }

        public IList<GenCapacity> GetGenCapacities() {
            return Session.QueryOver<GenCapacity>().OrderBy(m=>m.OrderIndex).Asc.List();
        }
        public IList<GenCapacity> GetGenCapacities(int versionId) {
            var data = Session.QueryOver<GenCapacity>().OrderBy(m=>m.OrderIndex).Asc.List();
            applyUserEdits<GenCapacity>(data, versionId, (m)=>m.GetKey());
            return data;
        }
        #endregion

        #region AvailOrDemand
        public void Add(AvailOrDemand gen) {
            Session.Save(gen);
        }

        public void Delete(AvailOrDemand gen) {
            Session.Delete(gen);
        }

        public IList<AvailOrDemand> GetAvailOrDemands(ElsiGenDataType dataType) {
            return Session.QueryOver<AvailOrDemand>().Where( m=>m.DataType == dataType).List();
        }
        #endregion

        #region PeakDemand
        public void Add(PeakDemand obj) {
            Session.Save(obj);
        }

        public void Delete(PeakDemand obj) {
            Session.Delete(obj);
        }

        public IList<PeakDemand> GetProfilePeakDemands() {
            return Session.QueryOver<PeakDemand>().Where(m=>m.Scenario==null).List();
        }
        public IList<PeakDemand> GetPeakDemands() {
            return Session.QueryOver<PeakDemand>().Where(m=>m.Scenario!=null).List();
        }
        #endregion

        #region Link
        public void Add(Link obj) {
            Session.Save(obj);
        }
        public void Delete(Link obj) {
            Session.Delete(obj);
        }
        public IList<Link> GetLinks() {
            return Session.QueryOver<Link>().List();
        }
        #endregion

        public string LoadFromXlsm(IFormFile formFile) {
            var msg = "";
            using ( var da = new DataAccess() ) {
                var loader = new ElsiXlsmLoader(da);
                msg+=loader.Load(formFile) + "\n";
                da.CommitChanges();
            }
            return msg;
        }

        #region ElsiDataVersion
        public void Add(ElsiDataVersion dataVersion) {
            Session.Save(dataVersion);
        }

        public void Delete(ElsiDataVersion dataVersion) {
            
            // Delete children
            var children = Session.QueryOver<ElsiDataVersion>().Where(m=>m.Parent.Id == dataVersion.Id).List();
            foreach( var dv in children) {
                Delete(dv);
            }
            // Delete all user edits that reference this version
            var ues = Session.QueryOver<ElsiUserEdit>().Where( m=>m.Version.Id == dataVersion.Id).List();
            foreach( var ue in ues) {
                Session.Delete(ue);
            }
            // Delete all results that reference this version
            var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == dataVersion.Id).List();
            foreach( var er in ers) {
                Session.Delete(er);
            }
            Session.Delete(dataVersion);
        }

        public ElsiDataVersion GetDataVersion(int id) {
            return Session.Get<ElsiDataVersion>(id);
        }

        public ElsiDataVersion GetDataVersion(string name, int userId, int id) {
            return Session.QueryOver<ElsiDataVersion>().Where(m=>m.Name.IsLike(name) && m.Id!=id && m.User.Id==userId).Take(1).SingleOrDefault();
        }

        public IList<ElsiDataVersion> GetDataVersions(int userId) {
            return Session.QueryOver<ElsiDataVersion>().Where( m=>m.User.Id == userId).List();
        }
        public ElsiDataVersion GetDataVersion(int userId, string name) {
            return Session.QueryOver<ElsiDataVersion>().Where( m=>m.User.Id == userId).And(m=>m.Name.IsLike(name)).Take(1).SingleOrDefault();
        }

        public int[] GetAllDataVersionIds(int versionId) {

            var q = Session.QueryOver<ElsiDataVersion>().
                Where(m=>m.Id == versionId).
                Fetch(SelectMode.Fetch,m=>m.Parent);
            var dv = q.Take(1).SingleOrDefault();

            var dvs = new List<int>();
            while (dv!=null) {
                dvs.Add(dv.Id);
                dv = dv.Parent;
            }
            // Reverse so we have the root data version first and then incremental change up the
            // the version we are interested in
            dvs.Reverse();
            return dvs.ToArray();
        }
        #endregion

        #region ElsiUserEdit
        public IList<ElsiUserEdit> GetUserEdits(string tableName, int versionId) {
            var data = Session.QueryOver<ElsiUserEdit>().
                Where(m=>m.Version.Id == versionId).
                And(m=>m.TableName == tableName).
                List();
            return data;
        }


        public Dictionary<int,List<ElsiUserEdit>> GetUserEditsDict<T>(int[] versionIds) {
            var tableName = typeof(T).Name;
            var dict = new Dictionary<int,List<ElsiUserEdit>>();
            var all = Session.QueryOver<ElsiUserEdit>().
                Where(m=>m.Version.Id.IsIn(versionIds)).
                And(m=>m.TableName == tableName).
                List();
            foreach( var id in versionIds ) {
                var eds = all.Where(m=>m.Version.Id == id).ToList();
                dict.Add(id,eds);
            }
            return dict;
        }

        public void applyUserEdits<T>(IList<T> data,int versionId, Func<T,string> keyFcn) {
            // Gets list of all versions in order from root upwards
            var versionIds = GetAllDataVersionIds(versionId);
            // Gets dictionary of all user edits by version id
            var userEdits = GetUserEditsDict<T>(versionIds);
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
                foreach( var vId in versionIds) {
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

        public void SaveUserEdit(ElsiUserEdit userEdit) {
            var ue = userEdit.Id!=0 ? Session.Get<ElsiUserEdit>(userEdit.Id) : null;
            if ( ue!=null ) {
                ue.Value = userEdit.Value;
            } else {
                userEdit.Version = GetDataVersion(userEdit.VersionId);
                Session.Save(userEdit);
            }
            // remove existing results since they have been invalidated
            // Delete all results that reference this version
            var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == userEdit.VersionId).List();
            foreach( var er in ers) {
                Session.Delete(er);
            }
        }

        public void DeleteUserEdit(int userEditId) {
            var ue = Session.Get<ElsiUserEdit>(userEditId);
            if ( ue!=null ) {
                // remove existing results since they have been invalidated
                // Delete all results that reference this version
                var ers = Session.QueryOver<ElsiResult>().Where( m=>m.Dataset.Id == ue.Version.Id).List();
                foreach( var er in ers) {
                    Session.Delete(er);
                }
                Session.Delete(ue);
            }
        }


        #endregion

        #region ElsiResult

        public ElsiResult GetResult(int datasetId, int day, ElsiScenario scenario) {
            return Session.QueryOver<ElsiResult>().Where(m=>m.Dataset.Id == datasetId).
                    And(m=>m.Day == day).
                    And(m=>m.Scenario == scenario).
                    Take(1).SingleOrDefault();
        }
        public void Add(ElsiResult result) {
            Session.Save(result);
        }

        public IList<ElsiResult> GetResults(int datasetId, ElsiScenario scenario) {
            return Session.QueryOver<ElsiResult>().
                Where(m=>m.Dataset.Id == datasetId).
                And(m=>m.Scenario == scenario).
                OrderBy(m=>m.Day).Asc.List();
        }

        public int GetResultCount(int datasetId) {
            return Session.QueryOver<ElsiResult>().
                Where(m=>m.Dataset.Id == datasetId).
                RowCount();
        }

        public ElsiResult GetResult(int id) {
            return Session.QueryOver<ElsiResult>().Where(m=>m.Id == id).Take(1).SingleOrDefault();
        }
        #endregion

        #region MiscParam
        public MiscParams GetMiscParams() {
            var mp = Session.QueryOver<MiscParams>().Take(1).SingleOrDefault();
            if ( mp==null ) {
                mp = new MiscParams();
                Session.Save(mp);
            }
            return mp;
        }
        #endregion

        public void Delete<T>(int id) where T: class{

            var obj = Session.Get<T>(id);
            if ( obj!=null ) {
                Session.Delete(obj);
            }

        }


    }
}
