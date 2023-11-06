using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using Org.BouncyCastle.Asn1.Icao;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Web;

namespace SmartEnergyLabDataApi.Data
{
    public class Substations : DataSet
    {
        public Substations(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        public string LoadDistributionSubstations(IFormFile file)
        {
            var loader = new DistributionSubstationLoader(DataAccess);
            return loader.Load(file);
        }
        public void LoadFromSpreadsheet(string geographicalAreaName, IFormFile file)
        {
            var gan = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if ( gan==null ) {
                throw new Exception($"Unknow geographical area [{geographicalAreaName}]");
            }
            var loader = new SubstationXlsLoader(DataAccess, gan);
            loader.Load(file);
        }

        public void LoadPrimarySubstationsFromSpreadsheet(string geographicalAreaName, IFormFile file)
        {
            var gan = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if (gan == null) {
                throw new Exception($"Unknown geographical area [{geographicalAreaName}]");
            }
            var loader = new PrimarySubstationXlsLoader(DataAccess, gan);
            loader.Load(file);
        }
        public string LoadPrimarySubstations(string geographicalAreaName, IFormFile file)
        {
            var gan = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if (gan == null) {
                throw new Exception($"Unknow geographical area [{geographicalAreaName}]");
            }
            var loader = new PrimarySubstationLoader(DataAccess, gan);
            return loader.Load(file);
        }

        public int GetCustomersForPrimarySubstation(int id) {
            // Get ids of distribution substations attached to this primary
            var dssIds = Session.QueryOver<DistributionSubstation>().Where(m=>m.PrimarySubstation.Id==id).Select(m=>m.Id).List<int>().ToArray();
            // Find sum of number of customers 
            var sum = Session.QueryOver<DistributionSubstationData>().Where(m=>m.DistributionSubstation.Id.IsIn(dssIds)).SelectList(l=>l.SelectSum(m=>m.NumCustomers)).List<int?>();
            if ( sum.Count>0 && sum[0]!=null) {
                return (int) sum[0];
            } else {
                return 0;
            }
        }

        public class SubstationSearchResult {
            public SubstationSearchResult(int id, int parentId, string name, string type) {
                Id = id;
                Name = name;
                Type = type;
                ParentId = parentId;
            }
            public int Id {get; set;}
            public int ParentId {get; set;}
            public string Type {get; set;}
            public string Name {get; set;}            
        }

        public IList<SubstationSearchResult> Search(string str, int maxResults) {

            int mResults = maxResults/3;
            
            // Grid supply points
            var q1=Session.QueryOver<GridSupplyPoint>().
                Where( m=>m.Name.IsInsensitiveLike(str, MatchMode.Anywhere) || m.ExternalId.IsInsensitiveLike(str));
            var q1Results = q1.Take(mResults).List();
            var results=q1Results.Select(m=>new SubstationSearchResult(m.Id,0,m.Name,m.GetType().Name)).ToList();

            // Primary substations
            mResults = (maxResults - results.Count)/2;
            var q2=Session.QueryOver<PrimarySubstation>().
                Where( m=>m.Name.IsInsensitiveLike(str, MatchMode.Anywhere) || m.ExternalId.IsInsensitiveLike(str));
            var q2Results=q2.Take(mResults).List();
            var results2=q2Results.Select(m=>new SubstationSearchResult(m.Id,m.GridSupplyPoint.Id,m.Name,m.GetType().Name)).ToList();
            results.AddRange(results2);

            // Distribution substations
            mResults = maxResults - results.Count;
            var q3=Session.QueryOver<DistributionSubstation>().
                Where( m=>m.Name.IsInsensitiveLike(str, MatchMode.Anywhere) || m.ExternalId.IsInsensitiveLike(str));
            var q3Results=q3.Take(mResults).List();
            var results3=q3Results.Select(m=>new SubstationSearchResult(m.Id,m.PrimarySubstation.Id,m.Name,m.GetType().Name)).ToList();
            results.AddRange(results3);

            return results;
        }

        #region DistributionSubstations
        public void SetSubstationParams(int id, SubstationParams sParams) {
            var dss = Session.QueryOver<DistributionSubstation>().
                Where(m=>m.Id == id).Take(1).SingleOrDefault();
            if ( dss!=null) {
                if ( dss.ChargingParams==null ) {
                    dss.ChargingParams = new SubstationChargingParams(dss);
                }
                dss.SubstationParams.CopyFieldsFrom(sParams);
            }
        }

        public void SetSubstationChargingParams(int id, SubstationChargingParams sParams) {
            var dss = Session.QueryOver<DistributionSubstation>().
                Where(m=>m.Id == id).Take(1).SingleOrDefault();
            if ( dss!=null) {
                if ( dss.ChargingParams==null) {
                    dss.ChargingParams = new SubstationChargingParams(dss);
                }
                dss.ChargingParams.CopyFieldsFrom(sParams);
            }
        }
        public void SetSubstationHeatingParams(int id, SubstationHeatingParams sParams) {
            var dss = Session.QueryOver<DistributionSubstation>().
                Where(m=>m.Id == id).Take(1).SingleOrDefault();
            if ( dss!=null) {
                if ( dss.HeatingParams==null ) {
                    dss.HeatingParams = new SubstationHeatingParams(dss);
                }
                dss.HeatingParams.CopyFieldsFrom(sParams);
            }
        }

        public void AutoFillGISData(string geographicalAreaName)
        {
            var gan = DataAccess.Organisations.GetGeographicalArea(geographicalAreaName);
            if (gan == null) {
                throw new Exception($"Unknow geographical area [{geographicalAreaName}]");
            }
            var finder = new SubstationGISFinder(DataAccess,gan);
            finder.Find();
        }

        public void Add(DistributionSubstation ds)
        {
            Session.Save(ds);
        }
        public void Delete(DistributionSubstation ds)
        {
            Session.Delete(ds);
        }

        public DistributionSubstation GetDistributionSubstation(ImportSource source,string externalId, string externalId2=null, string name=null)
        {
            DistributionSubstation dss=null;
            if ( externalId!=null  ) {
                //?? Seems to have better peformance than using QueryOver ??
                //?? maybe because not doing joins as defined in DistributionsSubstation.cs
                dss = Session.Query<DistributionSubstation>().Where(m => m.Source==source && m.ExternalId == externalId).Take(1).SingleOrDefault();
            }
            if ( dss==null && externalId2!=null) {
                //?? Seems to have better pefromance than using QueryOver ??
                dss = Session.Query<DistributionSubstation>().Where(m => m.Source==source && m.ExternalId2 == externalId2).Take(1).SingleOrDefault();
            }
            if ( dss==null && name!=null) {
                //?? Seems to have better pefromance than using QueryOver ??
                dss = Session.Query<DistributionSubstation>().Where(m => m.Source==source && m.Name == name).Take(1).SingleOrDefault();
            }
            return dss;
        }

        public DistributionSubstation GetDistributionSubstation(string nr)
        {
            return Session.QueryOver<DistributionSubstation>().Where(m => m.NR == nr).Take(1).SingleOrDefault();
        }

        public DistributionSubstation GetDistributionSubstationByNRId(string nrId)
        {
            return Session.QueryOver<DistributionSubstation>().Where(m => m.NRId == nrId).Take(1).SingleOrDefault();
        }

        public DistributionSubstation GetDistributionSubstation(int id)
        {
            return Session.Get<DistributionSubstation>(id);
        }
        
        public IList<DistributionSubstation> GetDistributionSubstations(DistributionNetworkOperator dno)
        {
            // Seems to be much quicker than using joined tables
            var primaryIds= Session.QueryOver<PrimarySubstation>().Where(m=>m.DistributionNetworkOperator==dno).Select(m=>m.Id).List<int>().ToArray();
            var q = Session.QueryOver<DistributionSubstation>().
                Where(m => m.PrimarySubstation.Id.IsIn(primaryIds));
            var list = q.List();
            return list;
        }

        public IList<DistributionSubstation> GetDistributionSubstations()
        {
            return Session.QueryOver<DistributionSubstation>().List();
        }

        public IList<DistributionSubstation> GetDistributionSubstations(int skip, int take)
        {
            return Session.QueryOver<DistributionSubstation>().Skip(skip).Take(take).List();
        }

        public IList<DistributionSubstation> GetDistributionSubstationsByExternalIds(string[] externalIds)
        {
            var q = Session.QueryOver<DistributionSubstation>().Where( m=>m.ExternalId.IsIn(externalIds));
            return q.List();
        }


        public IList<DistributionSubstation> GetDistributionSubstationsByGAId(int gaId)
        {
            PrimarySubstation pss = null;
            var q = Session.QueryOver<DistributionSubstation>().
                Left.JoinAlias(m=>m.PrimarySubstation,()=>pss).
                Where(m => pss.GeographicalArea.Id == gaId);
            return q.List();
        }

        public IList<DistributionSubstation> GetDistributionSubstations(int primaryId)
        {
            var q = Session.QueryOver<DistributionSubstation>().
                Where(m => m.PrimarySubstation.Id == primaryId);
            return q.List();
        }

        public DistributionSubstation GetDistributionSubstation(int primaryId, string externalId)
        {
            var q = Session.QueryOver<DistributionSubstation>().
                Where(m => m.PrimarySubstation.Id == primaryId).And(m=>m.ExternalId==externalId);
            return q.Take(1).SingleOrDefault();
        }

        public IList<DistributionSubstation> GetDistributionSubstationsByGridSupplyPointId(int gspId)
        {
            // This method of fetching data is  much quicker than using table joins via aliases
            var psIds = Session.QueryOver<PrimarySubstation>().
                Where(m => m.GridSupplyPoint.Id == gspId).Select(m=>m.Id).List<int>().ToArray();
            var q = Session.QueryOver<DistributionSubstation>().
                Where(m => m.PrimarySubstation.Id.IsIn(psIds));
            var ls =  q.List();
            return ls;
        }

        public IList<DistributionSubstation> GetDistributionSubstationsWithNoGIS(GeographicalArea ga)
        {
            GISData gisData = null;
            PrimarySubstation pss = null;
            var q = Session.QueryOver<DistributionSubstation>().
                Left.JoinAlias(m => m.GISData, () => gisData).
                Left.JoinAlias(m => m.PrimarySubstation, () => pss).
                Where(m=>pss.GeographicalArea == ga).
                Where(m=>gisData.Latitude == 0 && gisData.Longitude == 0);

            return q.List();

        }

        public DistributionSubstation GetDistributionSubstation(GeographicalArea ga, string name)
        {
            PrimarySubstation pss = null;
            var q = Session.QueryOver<DistributionSubstation>().
                Left.JoinAlias(m => m.PrimarySubstation, () => pss).
                Where(m => pss.GeographicalArea == ga).
                Where(m => m.Name.IsInsensitiveLike(name));

            var dss = q.Take(1).SingleOrDefault();



            return dss;

        }

        public DistributionSubstation GetDistributionSubstationByNrOrName(string nr, string name)
        {
            var dss = Session.QueryOver<DistributionSubstation>().Where( m=>m.NR==nr).Take(1).SingleOrDefault();
            if ( dss!=null ) {
                return dss;
            } else {
                return Session.QueryOver<DistributionSubstation>().Where( m=>m.Name==name).Take(1).SingleOrDefault();
            }
        }

        public DistributionSubstation GetDistributionSubstationByNrIdOrName(string nrId, string name)
        {
            var dss = Session.QueryOver<DistributionSubstation>().Where( m=>m.NRId==nrId).Take(1).SingleOrDefault();
            if ( dss!=null ) {
                return dss;
            } else {
                return Session.QueryOver<DistributionSubstation>().Where( m=>m.Name==name).Take(1).SingleOrDefault();
            }
        }

        public IList<SubstationClassification> GetDistributionSubstationClassifications(int id)
        {
            var q = Session.QueryOver<SubstationClassification>().
                Where(m=>m.DistributionSubstation.Id == id);

            var list = q.List();
            return list;
        }

        public IList<SubstationClassification> GetDistributionSubstationClassifications(int[] ids)
        {
            var q = Session.QueryOver<SubstationClassification>().
                Where(m=>m.DistributionSubstation.Id.IsIn(ids));

            var list = q.List();
            return list;
        }

        public void CreateSubstationParams() {
            var dsss = Session.QueryOver<DistributionSubstation>().Where( m=>m.SubstationParams==null).List();
            foreach( var dss in dsss) {
                dss.SubstationParams = new SubstationParams(dss);
            }
        }

        public int GetNumDistributionSubstations(int gaId) {
            PrimarySubstation pss=null;
            var num = Session.QueryOver<DistributionSubstation>().Left.JoinAlias(m=>m.PrimarySubstation,()=>pss).Where( m=>pss.GeographicalArea.Id==gaId).RowCount();
            return num;
        }

        #endregion

        #region PrimarySubstations
        public void Add(PrimarySubstation ps)
        {
            Session.Save(ps);
        }

        public void Delete(PrimarySubstation ps)
        {
            Session.Delete(ps);
        }

        public PrimarySubstation GetPrimarySubstation(int id) {
            return Session.QueryOver<PrimarySubstation>().Where(m=>m.Id == id).Take(1).SingleOrDefault();
        }

        public PrimarySubstation GetPrimarySubstation(ImportSource source, string externalId, string externalId2=null, string name=null) {
            PrimarySubstation pss=null;
            if ( externalId!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where( m=>m.Source==source && m.ExternalId==externalId).Take(1).SingleOrDefault();
            }
            if ( pss==null && externalId2!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where(m=>m.Source==source && m.ExternalId2 == externalId2).Take(1).SingleOrDefault();
            } 
            if ( pss==null && name!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where(m=>m.Source==source && m.Name == name).Take(1).SingleOrDefault();
            } 
            return pss;
        }

        public PrimarySubstation GetPrimarySubstationLike(MatchMode matchMode, ImportSource source, string externalId, string externalId2=null, string name=null) {
            PrimarySubstation pss=null;
            if ( externalId!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where( m=>m.Source==source && m.ExternalId.IsLike(externalId,matchMode)).Take(1).SingleOrDefault();
            }
            if ( pss==null && externalId2!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where(m=>m.Source==source && m.ExternalId2.IsLike(externalId2,matchMode)).Take(1).SingleOrDefault();
            } 
            if ( pss==null && name!=null ) {
                pss = Session.QueryOver<PrimarySubstation>().Where(m=>m.Source==source && m.Name.IsLike(name,matchMode)).Take(1).SingleOrDefault();
            } 
            return pss;
        }

        public PrimarySubstation GetPrimarySubstation(string nr)
        {
            return Session.QueryOver<PrimarySubstation>().Where(m => m.NR == nr).Take(1).SingleOrDefault();
        }

        public PrimarySubstation GetPrimarySubstationByNrOrName(string nr, string name)
        {
            var pss=Session.QueryOver<PrimarySubstation>().Where(m => m.NR == nr).Take(1).SingleOrDefault();
            if ( pss!=null) {
                return pss;
            } else {
                return Session.QueryOver<PrimarySubstation>().Where(m => m.Name == name).Take(1).SingleOrDefault();
            }
        }
        public PrimarySubstation GetPrimarySubstationByNrIdOrName(string nrId, string name)
        {
            var pss=Session.QueryOver<PrimarySubstation>().Where(m => m.NRId == nrId).Take(1).SingleOrDefault();
            if ( pss!=null) {
                return pss;
            } else {
                return Session.QueryOver<PrimarySubstation>().Where(m => m.Name == name).Take(1).SingleOrDefault();
            }
        }
        public IList<PrimarySubstation> GetPrimarySubstations()
        {
            return Session.QueryOver<PrimarySubstation>().List();
        }

        public IList<PrimarySubstation> GetPrimarySubstations(DistributionNetworkOperator dno)
        {
            return Session.QueryOver<PrimarySubstation>().Where(m=>m.DistributionNetworkOperator==dno).List();
        }

        public IList<PrimarySubstation> GetPrimarySubstationsByGeographicalAreaId(int gaId)
        {
            return Session.QueryOver<PrimarySubstation>().
                Where(m => m.GeographicalArea.Id == gaId).
                Fetch(SelectMode.Fetch,m=>m.GISData).
                List();
        }

        public IList<PrimarySubstation> GetPrimarySubstationsByGridSupplyPointId(int gspId)
        {
            return Session.QueryOver<PrimarySubstation>().
                Where(m => m.GridSupplyPoint.Id == gspId).
                Fetch(SelectMode.Fetch,m=>m.GISData).
                List();
        }

        public int GetNumPrimarySubstations(int gaId) {
            var num = Session.QueryOver<PrimarySubstation>().Where( m=>m.GeographicalArea.Id==gaId).RowCount();
            return num;
        }

        #endregion

        #region SubstationClassification

        #endregion

        #region DistributionSubstationData
        #endregion
    }
      

    
    
    
}
