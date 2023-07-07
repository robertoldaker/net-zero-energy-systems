using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;

namespace SmartEnergyLabDataApi.Data
{
    public class DataAccess : DataAccessBase
    {

        public DataAccess() : base()
        {
            SimplusGridTool = new SimplusGridTool(this);           
            Substations = new Substations(this);            
            Organisations = new Organisations(this);
            SupplyPoints = new SupplyPoints(this);
            SubstationLoadProfiles = new SubstationLoadProfiles(this);
            SubstationClassifications = new SubstationClassifications(this);
            VehicleCharging = new VehicleCharging(this);
            Loadflow = new Loadflow(this);
            Elsi = new Elsi(this);
            Users = new Users(this);
            Admin = new Admin(this);
            GIS = new GIS(this);
        }

        public SimplusGridTool SimplusGridTool { get; private set; }
        public Substations Substations { get; private set; }
        public SubstationLoadProfiles SubstationLoadProfiles { get; private set; }
        public SubstationClassifications SubstationClassifications { get; private set; }
        public Organisations Organisations { get; private set; }
        public SupplyPoints SupplyPoints { get; private set; }
        public VehicleCharging VehicleCharging { get; private set; }
        public Loadflow Loadflow { get; private set; }
        public Elsi Elsi { get; private set; }
        public Users Users {get; private set;}
        public Admin Admin {get; private set;}
        public GIS GIS {get; private set;}

        public static void SchemaUpdated(int oldVersion, int newVersion)
        {
            if ( oldVersion<29 ) {
                updateSubstationIds();
            }
            if ( oldVersion<30) {
                updateSubstationClassifications();
            }
            if ( oldVersion<35) {
               //?? updateBoundaries();
            }
        }

        public static void updateBoundaries() {
            Logger.Instance.LogInfoEvent("Start updating boundaries ...");
            int numGISData;
            using ( var da = new DataAccess() ) {
                numGISData = da.GIS.GetGISDataCount();
            }
            Logger.Instance.LogInfoEvent($"Num boundaries = [{numGISData}]");
            //
            int take = 1000;
            for( int skip=0; skip<numGISData; skip+=take) {
                using ( var da = new DataAccess() ) {
                    var gisData = da.GIS.GetGISData(skip,take);
                    Logger.Instance.LogInfoEvent($"Updating boundaries [{skip}] to [{skip+gisData.Count}]");
                    foreach( var gd in gisData) {
                        var boundary = new GISBoundary(gd);
                        if ( gd.BoundaryLatitudes!=null ) {
                            boundary.Latitudes = new double[gd.BoundaryLatitudes.Length];
                            Array.Copy(gd.BoundaryLatitudes,boundary.Latitudes,boundary.Latitudes.Length);
                        }
                        if ( gd.BoundaryLongitudes!=null ) {
                            boundary.Longitudes = new double[gd.BoundaryLongitudes.Length];
                            Array.Copy(gd.BoundaryLongitudes,boundary.Longitudes,boundary.Longitudes.Length);
                        }
                        da.GIS.Add(boundary);
                    }
                    da.CommitChanges();
                }
            }
            Logger.Instance.LogInfoEvent("Finsihed updating boundaries ...");

        }

        private static void updateSubstationIds() {
            using( var da = new DataAccess() ) {
                var pss = da.Substations.GetPrimarySubstationsByGeographicalAreaId(1);
                foreach( var ps in pss) {
                    ps.NR = ps.ExternalId;
                    var dss = da.Substations.GetDistributionSubstations(ps.Id);
                    foreach( var ds in dss) {
                        ds.NR = ds.ExternalId;
                    }
                }
                da.CommitChanges();
            }
        }

        private static void updateSubstationClassifications() {
            using( var da = new DataAccess() ) {
                var scs = da.Session.QueryOver<SubstationClassification>().
                    Fetch(SelectMode.Fetch,m=>m.DistributionSubstation).
                    List();
                foreach( var sc in scs) {
                    if ( sc.DistributionSubstation.PrimarySubstation!=null ) {
                        sc.PrimarySubstation = sc.DistributionSubstation.PrimarySubstation;
                        if ( sc.PrimarySubstation.GridSupplyPoint!=null ) {
                            sc.GridSupplyPoint = sc.DistributionSubstation.PrimarySubstation.GridSupplyPoint;
                            if ( sc.PrimarySubstation.GridSupplyPoint.GeographicalArea!=null) {
                                sc.GeographicalArea = sc.DistributionSubstation.PrimarySubstation.GridSupplyPoint.GeographicalArea;
                            }
                        }
                    }
                }
                da.CommitChanges();
            }
        }
    }
}
