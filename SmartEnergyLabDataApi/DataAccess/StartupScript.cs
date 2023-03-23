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
            if (newVersion == 1) {
                script.createInitialIndexes();
                script.createDefaultDNOs();
                script.createDefaultGeographicalAreas();
            } else if (newVersion ==2 ) {
                script.createSubstationParams();
            } else if (newVersion ==3 ) {
                script.populateSubstationLoadProfileKeys();
            } else if ( newVersion==4) {
                script.updateSubstationClassifications();
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

        private void createDefaultDNOs()
        {
            using( var da = new DataAccess() ) {
                // All DNOs
                var dno = new DistributionNetworkOperator(
                    DNOCodes.EasternPowerNetworks, "Eastern Power Networks Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.ElectricityNorthWest, "Electricity North West Limited");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.LondonPowerNetworks, "London Power Networks Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.NorthernPowergridNorthEast, "Northern Powergrid (Northeast) Limited");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.NorthernPowergridYorkshire, "Northern Powergrid (Yorkshire) Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.ScottishHydroElectricPowerDistribution, "Scottish Hydro Electric Power Distribution Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.SouthEasternPowerNetworks, "South Eastern Power Networks Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.SouthernElectricPowerDistribution, "Southern Electric Power Distribution Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.SPDistribution, "SP Distribution Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.SPManweb, "SP Manweb Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.WesternPowerDistributionEastMidlands, "Western Power Distribution (East Midlands) Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.WesternPowerDistributionSouthWales, "Western Power Distribution (South Wales) Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.WesternPowerDistributionSouthWest, "Western Power Distribution (South West) Plc");
                da.Organisations.Add(dno);
                dno = new DistributionNetworkOperator(
                    DNOCodes.WesternPowerDistributionWestMidlands, "Western Power Distribution (West Midlands) Plc");
                da.Organisations.Add(dno);
                //
                da.CommitChanges();
            }
        }

        private void createDefaultGeographicalAreas()
        {
            using( var da = new DataAccess() ) {
                var dno = da.Organisations.GetDistributionNetworkOperator(DNOCodes.WesternPowerDistributionSouthWest);
                // Geographical area
                var ga = new GeographicalArea("Bath", dno);
                da.Organisations.Add(ga);
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
