using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class DatasetInfo {
        public DatasetInfo( DataAccess da, int datasetId) {
            var q1 = da.Session.QueryOver<GenParameter>();
            GenParameterInfo = new DatasetData<GenParameter>(da, datasetId,m=>m.Type.ToString(),q1);
            var q2 = da.Session.QueryOver<GenCapacity>().OrderBy(m=>m.OrderIndex).Asc;
            GenCapacityInfo = new DatasetData<GenCapacity>(da, datasetId,m=>m.GetKey(),q2);
            var q3 = da.Session.QueryOver<PeakDemand>();
            PeakDemandInfo = new DatasetData<PeakDemand>(da, datasetId,m=>m.GetKey(),q3);
            var q4 = da.Session.QueryOver<MiscParams>();
            MiscParamsInfo = new DatasetData<MiscParams>(da,datasetId,m=>m.GetKey(),q4);
            var q5 = da.Session.QueryOver<Link>();
            LinkInfo = new DatasetData<Link>(da,datasetId,m=>m.GetKey(),q5);
        }
        public DatasetData<GenParameter> GenParameterInfo {get; private set;}
        public DatasetData<GenCapacity> GenCapacityInfo {get; private set;}
        public DatasetData<PeakDemand> PeakDemandInfo {get; private set;}
        public DatasetData<MiscParams> MiscParamsInfo {get; private set;}
        public DatasetData<Link> LinkInfo {get; private set;}
    }
}