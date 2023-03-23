using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Web;

namespace SmartEnergyLabDataApi.Data
{
    public class SubstationClassifications : DataSet
    {
        public SubstationClassifications(DataAccess da) : base (da)
        {

        }
        public void Add(SubstationClassification ssClass)
        {
            Session.Save(ssClass);
        }

        public void Delete(SubstationClassification ssClass)
        {
            Session.Delete(ssClass);
        }
        
        public IList<SubstationClassification> GetSubstationClassifications(DistributionNetworkOperator dno)
        {
            PrimarySubstation pss = null;
            var q = Session.QueryOver<SubstationClassification>().
                Left.JoinAlias(m => m.PrimarySubstation, () => pss).
                Where(m => pss.DistributionNetworkOperator == dno).
                Fetch(SelectMode.Fetch, m => m.DistributionSubstation);
            return q.List();
        }
        
        public IList<SubstationClassification> GetSubstationClassifications(DistributionSubstation dss)
        {
            var q = Session.QueryOver<SubstationClassification>().Where(m => m.DistributionSubstation == dss);
            return q.List();
        }

        public IList<SubstationClassification> GetPrimarySubstationClassifications(int id, bool aggregateResults)
        {

            var l1 = new List<SubstationClassification>();
            for( int num=1; num<=8; num++) {
                var query = Session.QueryOver<SubstationClassification>().Where(m=>m.PrimarySubstation.Id == id);
                var sc = getAggregate(query, num);
                l1.Add(sc);
            }
            //
            return l1;
        }

        public IList<SubstationClassification> GetGeographicalAreaClassifications(int id, bool aggregateResults=false)
        {
            //
            var l1 = new List<SubstationClassification>();
            for( int num=1; num<=8; num++) {
                var query = Session.QueryOver<SubstationClassification>().Where(m=>m.GeographicalArea.Id == id);
                var sc = getAggregate(query, num);
                l1.Add(sc);
            }
            //
            return l1;
        }

        private SubstationClassification getAggregate( IQueryOver<SubstationClassification,SubstationClassification> q, int num) {
            q=q.Where( m=>m.Num == num);
            var sums = q.SelectList(l => l.SelectSum(m => m.NumberOfCustomers).
                                            SelectSum(m=>m.NumberOfEACs).
                                            SelectSum(m=>m.ConsumptionKwh) ).SingleOrDefault<object[]>();
            var sc = new SubstationClassification();
            sc.Num = num;
            sc.NumberOfCustomers = sums[0]!=null ? (int) sums[0]: 0;
            sc.NumberOfEACs = sums[1]!=null ? (int) sums[1] : 0;
            sc.ConsumptionKwh = sums[2]!=null ? (double) sums[2] : 0;
            return sc;
        }

    }
}

