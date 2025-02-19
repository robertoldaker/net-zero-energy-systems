using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Dialect.Schema;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.Data
{
    public class StartupScript
    {
        private static readonly List<DataAccessBase.DbIndex> _indexes = new List<DataAccessBase.DbIndex>() {
            // distribution_substations
            new DataAccessBase.DbIndex("ix_external_id","distribution_substations","externalid"),
            new DataAccessBase.DbIndex("ix_dss_source_externalId","distribution_substations","source","externalid"),
            new DataAccessBase.DbIndex("ix_dss_source_externalId2","distribution_substations","source","externalid2"),
            new DataAccessBase.DbIndex("ix_dss_source_name","distribution_substations","source","name"),
            new DataAccessBase.DbIndex("ix_distribution_substations_substationDataId","distribution_substations","distributionsubstationdataid"),
            new DataAccessBase.DbIndex("ix_distribution_substations_gisDataId","distribution_substations","gisdataid"),
            new DataAccessBase.DbIndex("ix_distribution_substations_substationParamsId","distribution_substations","substationparamsid"),
            new DataAccessBase.DbIndex("ix_distribution_substations_chargingParamsId","distribution_substations","chargingparamsid"),
            new DataAccessBase.DbIndex("ix_distribution_substations_heatingParamsId","distribution_substations","heatingparamsid"),
            // distribution_load_profiles
            new DataAccessBase.DbIndex("ix_day_month_num","substation_load_profiles","day","monthnumber"),
            new DataAccessBase.DbIndex("ix_year_source","substation_load_profiles","year","source"),
            new DataAccessBase.DbIndex("ix_substation_load_profiles_distributionSubstationId","substation_load_profiles","distributionsubstationid"),
            new DataAccessBase.DbIndex("ix_slp_distributionSubstationId_source_year","substation_load_profiles","distributionsubstationid","source","year"),
            new DataAccessBase.DbIndex("ix_slp_primarySubstationId_source_year","substation_load_profiles","primarysubstationid","source","year"),
            new DataAccessBase.DbIndex("ix_slp_gridSupplyPointId_source_year","substation_load_profiles","gridsupplypointid","source","year"),
            new DataAccessBase.DbIndex("ix_slp_geograhicalAreaId_source_year","substation_load_profiles","geographicalareaid","source","year"),
            //
            new DataAccessBase.DbIndex("ix_slp_distributionSubstationId_source","substation_load_profiles","distributionsubstationid","source"),
            new DataAccessBase.DbIndex("ix_slp_source_isdummy","substation_load_profiles","source","isdummy"),
            // distribtion_substation_data
            new DataAccessBase.DbIndex("ix_distribution_substation_data_distributionSubstationId","distribution_substation_data","distributionsubstationid"),
            // substation_charging_params
            new DataAccessBase.DbIndex("ix_substation_charging_params_distributionSubstationId","substation_charging_params","distributionsubstationid"),
            // substation_classifications
            new DataAccessBase.DbIndex("ix_substation_classifications_distributionSubstationId","substation_classifications","distributionsubstationid"),
            new DataAccessBase.DbIndex("ix_num","substation_classifications","num"),
            // substation_heating_params
            new DataAccessBase.DbIndex("ix_substation_heating_params_distributionSubstationId","substation_heating_params","distributionsubstationid"),

            // primary_substations
            new DataAccessBase.DbIndex("ix_pss_source_externalId","primary_substations","source","externalid"),
            new DataAccessBase.DbIndex("ix_pss_source_externalId2","primary_substations","source","externalid2"),
            new DataAccessBase.DbIndex("ix_pss_source_name","primary_substations","source","name"),

            // grid_supply_points
            new DataAccessBase.DbIndex("ix_gsp_source_externalId","grid_supply_points","source","externalid"),
            new DataAccessBase.DbIndex("ix_gsp_source_externalId2","grid_supply_points","source","externalid2"),
            new DataAccessBase.DbIndex("ix_gsp_source_name","grid_supply_points","source","name"),

            // gis_boundaries
            new DataAccessBase.DbIndex("ix_gis_boundaries_gisDataId","gis_boundaries","gisdataid"),

        };

        public static void RunNewVersion(int oldVersion, int newVersion)
        {
            var script = new StartupScript();
            // this gets running with a fresh db
            if (oldVersion<1) {
                //script.createInitialIndexes();
                //script.createIndexes2();
                //script.createIndexes3();
                script.createDefaultDNOs();
                script.createDefaultGeographicalAreas();
            }
            if (oldVersion<2 ) {
                script.createSubstationParams();
            }
            if (oldVersion<3 ) {
                script.populateSubstationLoadProfileKeys();
            }
            if ( oldVersion<4) {
                script.updateSubstationClassifications();
            }
            if ( oldVersion<5) {
                script.fixExistingDNOs();
                script.fixExistingGAs();
                script.createDefaultDNOs();
                script.createDefaultGeographicalAreas();
            }
            if ( oldVersion<6) {
                script.fixGridSupplyPoints();
                script.fixPrimaries();
                script.fixDistributions();
                //script.createIndexes2();
            }
            if ( oldVersion<7) {
                //script.createIndexes3();
            }
            if ( oldVersion<8) {
                script.updateLoadProfiles();
            }
            if ( oldVersion<9) {
                script.createDefaultDatasets();
            }
        }

        public static void RunStartup() {
            //
            var script = new StartupScript();
            script.createIndexes();
            script.createDefaultDNOs();
            script.createDefaultGeographicalAreas();
        }

        private void createDefaultDatasets() {
            //
            using( var da = new DataAccess() ) {
                var rootDs = da.Datasets.GetRootDataset(DatasetType.Elsi);
                if ( rootDs == null ) {
                    rootDs = new Dataset() {
                        Name = "Empty",
                        Type = DatasetType.Elsi
                    };
                    da.Datasets.Add(rootDs);
                }

                var name = "GB network";
                var ds = da.Datasets.GetDataset(DatasetType.Elsi,name);
                if ( ds==null ) {
                    ds = new Dataset() {
                        Name = name,
                        Type = DatasetType.Elsi,
                        Parent = rootDs
                    };
                    da.Datasets.Add(ds);
                    // Gen Capacities
                    var genCapacities = da.Elsi.GetRawData<GenCapacity>(m=>m.Dataset==null);
                    foreach( var gc in genCapacities) {
                        gc.Dataset = ds;
                    }
                    // Gen Parameters
                    var genParameters = da.Elsi.GetRawData<GenParameter>(m=>m.Dataset==null);
                    foreach( var gp in genParameters) {
                        gp.Dataset = ds;
                    }
                    // Links
                    var links = da.Elsi.GetRawData<Link>(m=>m.Dataset==null);
                    foreach( var l in links) {
                        l.Dataset = ds;
                    }
                    // Misc params
                    var miscParams = da.Elsi.GetRawData<MiscParams>(m=>m.Dataset==null);
                    foreach( var mp in miscParams) {
                        mp.Dataset = ds;
                    }
                    // Peak demands
                    var peakDemands = da.Elsi.GetRawData<PeakDemand>(m=>m.Dataset==null);
                    foreach( var pd in peakDemands) {
                        pd.Dataset = ds;
                    }                    
                }
                rootDs = da.Datasets.GetRootDataset(DatasetType.BoundCalc);
                if ( rootDs==null ) {
                    rootDs = new Dataset() {
                        Name = "Empty",
                        Type = DatasetType.BoundCalc
                    };
                    //
                    da.Datasets.Add(rootDs);
                }
                ds = da.Datasets.GetDataset(DatasetType.BoundCalc,name);
                if ( ds==null ) {
                    ds = new Dataset() {
                        Name = name,
                        Type = DatasetType.BoundCalc,
                        Parent = rootDs
                    };
                    da.Datasets.Add(ds);
                    // Boundaries
                    var boundaries = da.BoundCalc.GetRawData<BoundCalcBoundary>(m=>m.Dataset==null);
                    foreach( var b in boundaries) {
                        b.Dataset = ds;
                    }
                    // Boundary zone
                    var boundaryZones = da.BoundCalc.GetRawData<BoundCalcBoundaryZone>(m=>m.Dataset==null);
                    foreach( var bz in boundaryZones) {
                        bz.Dataset = ds;
                    }
                    // Branches
                    var branches = da.BoundCalc.GetRawData<BoundCalcBranch>(m=>m.Dataset==null);
                    foreach( var b in branches) {
                        b.Dataset = ds;
                    }
                    // Ctrls
                    var ctrls = da.BoundCalc.GetRawData<BoundCalcCtrl>(m=>m.Dataset==null);
                    foreach( var c in ctrls) {
                        c.Dataset = ds;
                    }
                    // Nodes
                    var nodes = da.BoundCalc.GetRawData<BoundCalcNode>(m=>m.Dataset==null);
                    foreach( var n in nodes) {
                        n.Dataset = ds;
                    }
                    // Zones
                    var zones = da.BoundCalc.GetRawData<BoundCalcZone>(m=>m.Dataset==null);
                    foreach( var z in zones) {
                        z.Dataset = ds;
                    }
                }
                //
                da.CommitChanges();
            }
        }

        private void createIndexes() {
            // Ensure indexes are created
            var newIndexes = DataAccessBase.CreateIndexesIfNotExist(_indexes);
            foreach( var index in newIndexes) {
                Logger.Instance.LogInfoEvent($"New index created [{index.Name}]");
            }
        }

        private void createInitialIndexes()
        {
            try {
                if ( DataAccess.DbConnection.DbProvider == DbProvider.MariaDb) {
                    DataAccess.RunSql("CREATE INDEX ix_external_id ON distribution_substations (ExternalId(100))");
                    DataAccess.RunSql("CREATE INDEX ix_day_month_num ON substation_load_profiles (Day,MonthNumber)");
                    DataAccess.RunSql("CREATE INDEX ix_num ON substation_classifications (Num)");
                } else if (DataAccess.DbConnection.DbProvider == DbProvider.PostgreSQL) {
                    DataAccess.RunSql("CREATE INDEX ix_external_id ON distribution_substations (externalid)");
                    DataAccess.RunSql("CREATE INDEX ix_day_month_num ON substation_load_profiles (day,monthnumber)");
                    DataAccess.RunSql("CREATE INDEX ix_year_source ON substation_load_profiles (year,source)");
                    DataAccess.RunSql("CREATE INDEX ix_num ON substation_classifications (num)");
                }
            }
            catch (Exception e) {
                Logger.Instance.LogErrorEvent($"Error creating initial indexes [{e.Message}]");
            }
        }

        private void fixGridSupplyPoints() {
            using ( var da = new DataAccess() ) {
                var gsps = da.SupplyPoints.GetGridSupplyPoints();
                foreach( var gsp in gsps) {
                    gsp.Source = ImportSource.NationalGridDistributionOpenData;
                    gsp.ExternalId = gsp.NR;
                    gsp.ExternalId2 = gsp.NRId;
                }
                da.CommitChanges();
            }
        }

        private void fixPrimaries() {
            using ( var da = new DataAccess() ) {
                var psss = da.Substations.GetPrimarySubstations();
                foreach( var pss in psss) {
                    pss.Source = ImportSource.NationalGridDistributionOpenData;
                    pss.ExternalId = pss.NR;
                    pss.ExternalId2 = pss.NRId;
                }
                da.CommitChanges();
            }
        }

        private void fixDistributions() {
            using ( var da = new DataAccess() ) {
                var dsss = da.Substations.GetDistributionSubstations();
                foreach( var dss in dsss) {
                    dss.Source = ImportSource.NationalGridDistributionOpenData;
                    dss.ExternalId = dss.NR;
                    dss.ExternalId2 = dss.NRId;
                }
                da.CommitChanges();
            }
        }

        private void createIndexes2()
        {
            try {
                if (DataAccess.DbConnection.DbProvider == DbProvider.PostgreSQL) {
                    // dist substations
                    DataAccess.RunSql("CREATE INDEX ix_dss_source_externalId ON distribution_substations (source,externalid)");
                    DataAccess.RunSql("CREATE INDEX ix_dss_source_externalId2 ON distribution_substations (source,externalid2)");
                    DataAccess.RunSql("CREATE INDEX ix_dss_source_name ON distribution_substations (source,name)");
                    // primary substations
                    DataAccess.RunSql("CREATE INDEX ix_pss_source_externalId ON primary_substations (source,externalid)");
                    DataAccess.RunSql("CREATE INDEX ix_pss_source_externalId2 ON primary_substations (source,externalid2)");
                    DataAccess.RunSql("CREATE INDEX ix_pss_source_name ON primary_substations (source,name)");
                    // grid supply points
                    DataAccess.RunSql("CREATE INDEX ix_gsp_source_externalId ON grid_supply_points (source,externalid)");
                    DataAccess.RunSql("CREATE INDEX ix_gsp_source_externalId2 ON grid_supply_points (source,externalid2)");
                    DataAccess.RunSql("CREATE INDEX ix_gsp_source_name ON grid_supply_points (source,name)");
                }
            }
            catch (Exception e) {
                Logger.Instance.LogErrorEvent($"Error creating initial indexes [{e.Message}]");
            }
        }

        private void createIndexes3()
        {
            try {
                if (DataAccess.DbConnection.DbProvider == DbProvider.PostgreSQL) {
                    // dist substations
                    DataAccess.RunSql("CREATE INDEX ix_gis_boundaries_gisDataId ON gis_boundaries (gisdataid)");
                }
            }
            catch (Exception e) {
                Logger.Instance.LogErrorEvent($"Error creating initial indexes [{e.Message}]");
            }
        }

        private void createIndexes4()
        {
            try {
                if (DataAccess.DbConnection.DbProvider == DbProvider.PostgreSQL) {
                    // Links to primary substations
                    DataAccess.RunSql("CREATE INDEX ix_gis_data_primarySubstationId ON gis_data (primarysubstationid)");
                    
                    // Dist substations table (with cascade="all_delete_orphan")
                    // SubstationData
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substations_substationDataId ON distribution_substations (distributionsubstationdataid)");
                    // GISData                    
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substations_gisDataId ON distribution_substations (gisdataid)");
                    // SubstationParamsId
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substations_substationParamsId ON distribution_substations (substationparamsid)");
                    // ChargingParams
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substations_chargingParamsId ON distribution_substations (chargingparamsid)");
                    // HeatingParamsId
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substations_heatingParamsId ON distribution_substations (heatingparamsid)");
                    // Links to Distribution substations
                    DataAccess.RunSql("CREATE INDEX ix_gis_data_distributionSubstationId ON gis_data (distributionsubstationid)");
                    DataAccess.RunSql("CREATE INDEX ix_distribution_substation_data_distributionSubstationId ON distribution_substation_data (distributionsubstationid)");
                    DataAccess.RunSql("CREATE INDEX ix_substation_charging_params_distributionSubstationId ON substation_charging_params (distributionsubstationid)");
                    DataAccess.RunSql("CREATE INDEX ix_substation_classifications_distributionSubstationId ON substation_classifications (distributionsubstationid)");
                    DataAccess.RunSql("CREATE INDEX ix_substation_heating_params_distributionSubstationId ON substation_heating_params (distributionsubstationid)");
                    DataAccess.RunSql("CREATE INDEX ix_substation_load_profiles_distributionSubstationId ON substation_load_profiles (distributionsubstationid)");
                }
            }
            catch (Exception e) {
                Logger.Instance.LogErrorEvent($"Error creating initial indexes [{e.Message}]");
            }
        }

        private void updateLoadProfiles()
        {
            try {
                Logger.Instance.LogInfoEvent("Started updating load profiles");
                using( var da = new DataAccess() ) {
                    var name = "Melksham  S.G.P.";
                    var gsp = da.SupplyPoints.GetGridSupplyPointByName(name);
                    if ( gsp==null) {
                        throw new Exception($"Could not find GSP with name =[{name}]");
                    }
                    var lps=da.SubstationLoadProfiles.GetAllLoadProfilesByGridSupplyPoint(null);
                    foreach( var lp in lps) {
                        lp.GridSupplyPoint = gsp;
                    }
                    da.CommitChanges();
                }
                Logger.Instance.LogInfoEvent("Finished updating load profiles");
            }
            catch (Exception e) {
                Logger.Instance.LogErrorEvent($"Error updating load profiles [{e.Message}]");
            }
        }

        private void fixExistingDNOs() {
            using( var da = new DataAccess() ) {
                var dnos = da.Organisations.GetDistributionNetworkOperators();
                foreach( var dno in dnos) {
                    if ( (int) dno.Code==12) {
                        var defaultDNO = DefaultDNO.Values.Where(m=>m.Code==DNOCode.NationalGridElectricityDistribution).FirstOrDefault();
                        dno.Code = defaultDNO.Code;
                        dno.Name = defaultDNO.Name;
                    } else {
                        da.Organisations.Delete(dno);
                    }
                }
                //
                da.CommitChanges();
            }
        }

        private void fixExistingGAs() {
            using( var da = new DataAccess() ) {
                var ga = da.Organisations.GetGeographicalArea("South West");
                var defaultDNO = DefaultArea.Values.Where(m=>m.Area == DNOAreas.SouthWestEngland).FirstOrDefault();
                ga.Name = defaultDNO.Name;
                ga.DNOArea = defaultDNO.Area;
                //
                da.CommitChanges();
            }
        }

        private void createDefaultDNOs()
        {
            using( var da = new DataAccess() ) {
                foreach( var dDno in DefaultDNO.Values) {
                    var dno = da.Organisations.GetDistributionNetworkOperator(dDno.Code);
                    if ( dno==null) {
                        dno = new DistributionNetworkOperator(dDno.Code,dDno.Name);
                        da.Organisations.Add(dno);
                    }
                }
                //
                da.CommitChanges();
            }
        }

        private void createDefaultGeographicalAreas()
        {
            using( var da = new DataAccess() ) {
                foreach( var dArea in DefaultArea.Values) {
                    var dno = da.Organisations.GetDistributionNetworkOperator(dArea.Code);
                    var ga = da.Organisations.GetGeographicalArea(dArea.Area);
                    if ( ga==null ) {
                        ga = new GeographicalArea(dArea.Area,dArea.Name,dno);
                        da.Organisations.Add(ga);
                    }
                }
                //
                da.CommitChanges();
            }
        }

        private void createSubstationParams() {
            using (var da = new DataAccess() ) {
                da.Substations.CreateSubstationParams();
                da.CommitChanges();
            }
        }

        private void populateSubstationLoadProfileKeys() {
            using (var da = new DataAccess() ) {
                da.SubstationLoadProfiles.PopulateMissingKeys();
                da.CommitChanges();
            }
        }

        private void updateSubstationClassifications() {
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
