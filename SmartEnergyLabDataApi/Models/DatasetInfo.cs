using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class DatasetInfo {
        public DatasetInfo( DataAccess da, int versionId) {
            GenParameterInfo = new TableInfo<GenParameter>(da, versionId,m=>m.Type.ToString());
            GenCapacityInfo = new TableInfo<GenCapacity>(da, versionId,m=>m.GetKey());
            PeakDemandInfo = new TableInfo<PeakDemand>(da, versionId,m=>m.GetKey());
            MiscParamsInfo = new TableInfo<MiscParams>(da,versionId,m=>m.GetKey());
            LinkInfo = new TableInfo<Link>(da,versionId,m=>m.GetKey());
        }
        public TableInfo<GenParameter> GenParameterInfo {get; private set;}
        public TableInfo<GenCapacity> GenCapacityInfo {get; private set;}
        public TableInfo<PeakDemand> PeakDemandInfo {get; private set;}
        public TableInfo<MiscParams> MiscParamsInfo {get; private set;}
        public TableInfo<Link> LinkInfo {get; private set;}
    }

    public class TableInfo<T> where T: class {

        public TableInfo(DataAccess da, int versionId, Func<T,string> keyFcn) {
            Data = da.Elsi.GetData<T>(versionId,keyFcn);
            TableName = typeof(T).Name;
            UserEdits = da.Elsi.GetUserEdits(TableName, versionId);
        }
        public string TableName {get; private set;}
        public IList<T> Data {get; private set;}
        public IList<ElsiUserEdit> UserEdits{get; private set;}

    }

}