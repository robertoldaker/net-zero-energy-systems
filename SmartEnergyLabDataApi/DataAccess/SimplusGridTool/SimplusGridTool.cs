using HaloSoft.DataAccess;

namespace SmartEnergyLabDataApi.Data
{
    public class SimplusGridTool : DataSet
    {
        public SimplusGridTool(DataAccess da) : base(da)
        {

        }

        public IList<SGT.Model> GetModels()
        {
            var q = Session.QueryOver<SGT.Model>();
            return q.List();
        }
    }
}
