using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;

namespace SmartEnergyLabDataApi.Data
{
    public class StartupScript
    {

        public static void Run(int oldVersion, int newVersion)
        {
            var script = new StartupScript();
            if (oldVersion<1) {
                script.createInitialIndexes();
                script.createDefaultDNOs();
                script.createDefaultGeographicalAreas();
            } else if (oldVersion<2 ) {
                script.createSubstationParams();
            } else if (oldVersion <3 ) {
                script.populateSubstationLoadProfileKeys();
            } else if ( oldVersion<4) {
                script.updateSubstationClassifications();
            } else if ( oldVersion<5) {
                script.fixExistingDNOs();
                script.fixExistingGAs();
                script.createDefaultDNOs();
                script.createDefaultGeographicalAreas();
            } else if ( oldVersion<6) {
                script.fixGridSupplyPoints();
                script.fixPrimaries();
                script.fixDistributions();
                script.createIndexes2();
            } else if ( oldVersion<7) {
                script.createIndexes3();
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
