using System;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace EnergySystemLabDataApi
{
    public class DataModel {

        public void Load() {
            Rows = new List<DataRow>();
            using (var da = new DataAccess() ) {
                DataAccess.RunPostgreSQLQuery("SELECT pg_size_pretty(pg_database_size('smart_energy_lab'));",(row)=>{
                    this.Size = row[0].ToString();
                });
                var geos = da.Organisations.GetGeographicalAreas();
                foreach( var geo in geos) {
                    Rows.Add( new DataRow(da,geo));
                }
            }
        }

        public List<DataRow> Rows {get; private set;}
        public string Size {get; private set;}
    }

    public class DataRow {
        public DataRow(DataAccess da, GeographicalArea geo) {
            GeoGraphicalAreaId = geo.Id;
            GeoGraphicalArea = geo.Name;
            DNOIconUrl = getIconUrl(geo.DistributionNetworkOperator);
            DNO = geo.DistributionNetworkOperator.Name;
            NumGsps = da.SupplyPoints.GetNumGridSupplyPoints(geo.Id);
            NumPrimary = da.Substations.GetNumPrimarySubstations(geo.Id);
            NumDist = da.Substations.GetNumDistributionSubstations(geo.Id);
        }

        private string getIconUrl(DistributionNetworkOperator dno) {
            string root = "/assets/images/";
            if (  dno.Code == DNOCode.NationalGridElectricityDistribution) {
                return root + "national-grid-icon.png";
            } else if ( dno.Code == DNOCode.UKPowerNetworks ) {
                return root + "uk-power-networks-icon.png";
            } else if ( dno.Code == DNOCode.ElectricityNorthWest) {
                return root + "electricity-north-west-icon.png";
            } else if ( dno.Code == DNOCode.NorthernPowerGrid) {
                return root + "northern-power-grid-icon.png";
            } else if ( dno.Code == DNOCode.ScottishAndSouthernElectricityNetworks) {
                return root + "scottish-and-southern-electricity-networks-icon.png";
            } else if ( dno.Code == DNOCode.SPEnergyNetworks) {
                return root + "sp-electricity-networks-icon.png";
            } else {
                throw new Exception($"Unexpected DNO code [{dno.Code}] for dno [{dno.Name}]");
            }
        }
        public int GeoGraphicalAreaId {get; set;}
        public string GeoGraphicalArea {get; set;}
        public string DNOIconUrl {get; set;}
        public string DNO {get; set;}
        public int NumGsps {get; set;}
        public int NumPrimary {get; set;}
        public int NumDist {get; set;}
    }

    
}