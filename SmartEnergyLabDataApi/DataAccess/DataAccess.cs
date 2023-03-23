using HaloSoft.DataAccess;
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

        public static void SchemaUpdated(int oldVersion, int newVersion)
        {
            if ( oldVersion<29 ) {
                updateSubstationIds();
            }
            if ( oldVersion<30) {
                updateSubstationClassifications();
            }
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
