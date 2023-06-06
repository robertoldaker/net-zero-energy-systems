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
            GeoGraphicalArea = geo.Name;
            DNO = geo.DistributionNetworkOperator.Name;
            NumGsps = da.SupplyPoints.GetNumGridSupplyPoints(geo.Id);
            NumPrimary = da.Substations.GetNumPrimarySubstations(geo.Id);
            NumDist = da.Substations.GetNumDistributionSubstations(geo.Id);
        }
        public string GeoGraphicalArea {get; set;}
        public string DNO {get; set;}
        public int NumGsps {get; set;}
        public int NumPrimary {get; set;}
        public int NumDist {get; set;}
    }

    
}