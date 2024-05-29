using System.Collections.Immutable;
using System.Security.Cryptography;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using Org.BouncyCastle.Crypto.Signers;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

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
            NationalGrid = new NationalGrid(this);
            SolarInstallations = new SolarInstallations(this);
            Datasets = new Datasets(this);
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
        public NationalGrid NationalGrid { get; private set;}
        public SolarInstallations SolarInstallations { get; private set;}
        public Datasets Datasets { get; private set;}
        public static void SchemaUpdated(int oldVersion, int newVersion)
        {
            if ( oldVersion<29 ) {
                updateSubstationIds();
            }
            if ( oldVersion<30) {
                updateSubstationClassifications();
            }
            if ( oldVersion<44) {
                updateDistributionSubstationLinks();
            }
        }

        private static void updateDistributionSubstationLinks() {
            int take = 1000;
            int count;
            Logger.Instance.LogInfoEvent("Started updating distribution substation links ...");
            do {
                using( var da = new DataAccess()) {
                    var dsss = da.Substations.GetFirstUnlinkedDistributionSubstations(take,out count);
                    foreach( var dss in dsss) {
                        dss.GeographicalArea = dss.PrimarySubstation.GeographicalArea;
                        dss.GridSupplyPoint = dss.PrimarySubstation.GridSupplyPoint;
                    }
                    da.CommitChanges();
                    Logger.Instance.LogInfoEvent($"Updated distribution substation links, [{count}] left");
                }
            } while( count>0);
            Logger.Instance.LogInfoEvent("Finished updating distribution substation links");
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
