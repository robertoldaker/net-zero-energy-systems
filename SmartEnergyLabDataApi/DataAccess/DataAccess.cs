using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using SmartEnergyLabDataApi.Data.BoundCalc;

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
            BoundCalc = new BoundCalcDS(this);
            Elsi = new Elsi(this);
            Users = new Users(this);
            Admin = new Admin(this);
            GIS = new GIS(this);
            NationalGrid = new NationalGrid(this);
            SolarInstallations = new SolarInstallations(this);
            Datasets = new Datasets(this);
            Elexon = new Elexon(this);
        }

        public SimplusGridTool SimplusGridTool { get; private set; }
        public Substations Substations { get; private set; }
        public SubstationLoadProfiles SubstationLoadProfiles { get; private set; }
        public SubstationClassifications SubstationClassifications { get; private set; }
        public Organisations Organisations { get; private set; }
        public SupplyPoints SupplyPoints { get; private set; }
        public VehicleCharging VehicleCharging { get; private set; }
        public BoundCalcDS BoundCalc { get; private set; }
        public Elsi Elsi { get; private set; }
        public Users Users {get; private set;}
        public Admin Admin {get; private set;}
        public GIS GIS {get; private set;}
        public NationalGrid NationalGrid { get; private set;}
        public SolarInstallations SolarInstallations { get; private set;}
        public Datasets Datasets { get; private set;}
        public Elexon Elexon { get; private set; }
        public static void SchemaUpdated(int oldVersion, int newVersion)
        {
            if (oldVersion < 29) {
                updateSubstationIds();
            }
            if (oldVersion < 30) {
                updateSubstationClassifications();
            }
            if (oldVersion < 44) {
                updateDistributionSubstationLinks();
            }
            if (oldVersion < 57) {
                updateLoadflowCtrls();
            }
            if (oldVersion < 58) {
                updateGridSubstationLocations();
            }
            if (oldVersion < 59) {
                updateLoadflowBranches();
            }
            if (oldVersion < 60) {
                updateElsiScenarioData();
            }
            if (oldVersion < 61) {
                removeGISColumns();
            }
        }

        private static void removeGISColumns() {
            DataAccessBase.DeleteColumn("gis_data","GeographicalAreaId");
            DataAccessBase.DeleteColumn("gis_data","DistributionSubstationId");
            DataAccessBase.DeleteColumn("gis_data","PrimarySubstationId");
            DataAccessBase.DeleteColumn("gis_data","VehicleChargingStationId");
        }

        private static void updateElsiScenarioData() {
            using( var da = new DataAccess() ) {
                var genCapacities = da.Elsi.GetGenCapacities();
                var crs = genCapacities.Where( m=>m.Scenario == ElsiScenario.CommunityRenewables).ToList();
                foreach( var cr in crs ) {
                    cr.CommunityRenewables = cr.Capacity;
                    // look for others
                    var crgc = genCapacities.Where(m=>m.GenType == cr.GenType && m.Zone == cr.Zone ).ToList();
                    // fill in columns for each scenario
                    var twoDegrees = crgc.Where(m=>m.Scenario == ElsiScenario.TwoDegrees).FirstOrDefault();
                    if ( twoDegrees!=null ) {
                        cr.TwoDegrees = twoDegrees.Capacity;
                        da.Elsi.Delete(twoDegrees);
                    }
                    var steadyProgression = crgc.Where(m=>m.Scenario == ElsiScenario.SteadyProgression).FirstOrDefault();
                    if ( steadyProgression!=null ) {
                        cr.SteadyProgression = steadyProgression.Capacity;
                        da.Elsi.Delete(steadyProgression);
                    }
                    var consumerEvolution = crgc.Where(m=>m.Scenario == ElsiScenario.ConsumerEvolution).FirstOrDefault();
                    if ( consumerEvolution!=null ) {
                        cr.ConsumerEvolution = consumerEvolution.Capacity;
                        da.Elsi.Delete(consumerEvolution);
                    }
                }
                //
                var peakDemands = da.Elsi.GetPeakDemands();
                var pds = peakDemands.Where( m=>m.Scenario == ElsiScenario.CommunityRenewables).ToList();
                foreach( var cr in pds ) {
                    cr.CommunityRenewables = cr.Peak;
                    // look for others
                    var crpd = peakDemands.Where(m=>m.MainZone == cr.MainZone && m.Profile == cr.Profile).ToList();
                    var twoDegrees = crpd.Where(m=>m.Scenario == ElsiScenario.TwoDegrees).FirstOrDefault();
                    if ( twoDegrees!=null ) {
                        cr.TwoDegrees = twoDegrees.Peak;
                        da.Elsi.Delete(twoDegrees);
                    }
                    var steadyProgression = crpd.Where(m=>m.Scenario == ElsiScenario.SteadyProgression).FirstOrDefault();
                    if ( steadyProgression!=null ) {
                        cr.SteadyProgression = steadyProgression.Peak;
                        da.Elsi.Delete(steadyProgression);
                    }
                    var consumerEvolution = crpd.Where(m=>m.Scenario == ElsiScenario.ConsumerEvolution).FirstOrDefault();
                    if ( consumerEvolution!=null ) {
                        cr.ConsumerEvolution = consumerEvolution.Peak;
                        da.Elsi.Delete(consumerEvolution);
                    }
                }
                //
                da.CommitChanges();

            }
        }

        private static void updateLoadflowBranches() {
            Logger.Instance.LogInfoEvent($"Starting updating branches");
            using( var da = new DataAccess() ) {
                var ctrls = da.Session.QueryOver<Ctrl>().List();
                foreach( var ctrl in ctrls) {
                    if ( ctrl.Branch!=null) {
                        ctrl.Branch.SetCtrl(ctrl);
                    } else {
                        Logger.Instance.LogInfoEvent($"Ctrl found with no branch ref. [{ctrl.LineName}]");
                    }
                }
                //
                da.CommitChanges();
            }
            using( var da = new DataAccess() ) {
                var branches = da.Session.QueryOver<Branch>().List();
                foreach( var branch in branches) {
                    branch.SetType();
                }
                da.CommitChanges();
            }
            Logger.Instance.LogInfoEvent($"Finished updating branches");
        }

        private static void updateGridSubstationLocations() {
            using( var da = new DataAccess() ) {
                var datasets = da.Session.QueryOver<Dataset>().List();
                foreach( var ds in datasets) {
                    var nodes = da.Session.QueryOver<Node>().Where( m=>m.Dataset.Id == ds.Id ).List();
                    var locDict = new Dictionary<int,GridSubstationLocation>();
                    foreach( var n in nodes) {
                        var loc = n.Location;
                        if ( loc!=null ) {
                            if ( loc.Dataset==null || loc.Dataset.Id != ds.Id) {
                                if ( !locDict.ContainsKey(loc.Id) ) {
                                    var newLoc = loc.Copy(ds);
                                    da.NationalGrid.Add(newLoc);
                                    locDict.Add(loc.Id,newLoc);
                                    n.Location = newLoc;
                                } else {
                                    n.Location = locDict[loc.Id];
                                }
                            }
                        }
                    }
                }
                //
                da.CommitChanges();
            }
        }

        private static void updateLoadflowCtrls() {
            using ( var da = new DataAccess() ) {
                var ctrls = da.Session.QueryOver<Ctrl>().List();
                foreach ( var c in ctrls) {
                    if ( c.Branch==null && !string.IsNullOrEmpty(c.old_Code) ) {
                        var b = da.Session.QueryOver<Branch>().Where( m=>m.Code == c.old_Code && c.Dataset.Id == m.Dataset.Id).Take(1).SingleOrDefault();
                        if ( b!=null) {
                            c.Branch = b;
                            c.old_Node1 = null;
                            c.old_Node2 = null;
                        } else {
                            Logger.Instance.LogInfoEvent($"Could not find branch for ctrl [{c.LineName}]");
                        }
                    }
                }
                //
                da.CommitChanges();
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
