using System.Reflection;
using System.Text.Json;
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

        public IList<T> GetRawData<T>(System.Linq.Expressions.Expression<Func<T,bool>> whereFcn) where T: class{
            //
            var q = Session.QueryOver<T>().Where(whereFcn);

            return q.List();
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

        public void Delete(ElsiResult result) {
            Session.Delete(result);
        }

        public IList<ElsiResult> GetResults(int datasetId, ElsiScenario scenario) {
            return Session.QueryOver<ElsiResult>().
                Where(m=>m.Dataset.Id == datasetId).
                And(m=>m.Scenario == scenario).
                OrderBy(m=>m.Day).Asc.List();
        }

        public IList<ElsiResult> GetResultsWithData(int datasetId, ElsiScenario scenario) {
            return Session.QueryOver<ElsiResult>().
                Fetch(SelectMode.FetchLazyProperties,m=>m).
                Where(m=>m.Dataset.Id == datasetId).
                And(m=>m.Scenario == scenario).
                OrderBy(m=>m.Day).Asc.List();
        }

        public int GetResultCount(int datasetId) {
            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            return Session.QueryOver<ElsiResult>().
                Where(m=>m.Dataset.Id.IsIn(dsIds)).
                RowCount();
        }

        public void DeleteResults(int datasetId) {
            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var results = Session.QueryOver<ElsiResult>().
                Where(m=>m.Dataset.Id.IsIn(dsIds)).
                List();
            foreach( var r in results) {
                Delete(r);
            }
        }

        public ElsiResult GetResult(int id) {
            return Session.QueryOver<ElsiResult>().Where(m=>m.Id == id).Take(1).SingleOrDefault();
        }

        public List<ElsiDayResult> GetElsiDayResults( int datasetId, ElsiScenario scenario) {
            var ers = GetResultsWithData(datasetId, scenario);
            var edrs = new List<ElsiDayResult>();
            foreach( var er in ers) {
                var erStr = System.Text.Encoding.UTF8.GetString(er.Data);
                var edr = JsonSerializer.Deserialize<ElsiDayResult>(erStr,new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if ( edr!=null ){
                    edrs.Add(edr);
                }
            }
            return edrs;
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
