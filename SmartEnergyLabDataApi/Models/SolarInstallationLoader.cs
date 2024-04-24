using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class SolarInstallationLoader {

    public SolarInstallationLoader() {

    }

    public string Load(IFormFile file) {
        int numNew = 0;
        int numIgnored=0;
        var toAdd = new List<SolarInstallation>();
        using ( var da = new DataAccess() ) {
            using (var stream = file.OpenReadStream()) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nDss = geoJson.features.Length;
                var gspName = "Melksham  S.G.P.";
                var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspName);
                if ( gsp==null) {
                    throw new Exception($"Could not find gsp [{gspName}]");
                }
                foreach( var feature in geoJson.features) {
                    var name = feature.properties.Name;
                    if ( !int.TryParse(name, out int year) ) {
                        numIgnored++;
                        Logger.Instance.LogInfoEvent($"Could not parse name for solar installation [{name}]");
                        continue;
                    }
                    // Ensure its the full year
                    if ( year<100) {
                        year+=2000;
                    }
                    var lng = feature.geometry.coordinates[0];
                    var lat = feature.geometry.coordinates[1];
                    //
                    var folderPath = feature.properties.FolderPath;
                    var cpnts= folderPath.Split("/");
                    var substationName = cpnts[cpnts.Length-1];
                    //
                    var si = da.SolarInstallations.GetSolarInstallation(year, lat,lng);
                    if ( si==null ) {                        
                        var dss = da.Substations.GetDistributionSubstation(gsp,substationName);
                        //
                        if ( dss==null) {
                            numIgnored++;
                            Logger.Instance.LogInfoEvent($"Could not find dist substation [{substationName}]");
                            continue;                        
                        }
                        si = new SolarInstallation(year, dss, lat, lng);
                        toAdd.Add(si);
                        numNew++;
                    }                     
                }
                // Add new ones to db
                foreach( var si in toAdd) {
                    da.SolarInstallations.Add(si);
                }
            }

            // save changes
            da.CommitChanges();

        }

        var msg = $"[{numNew}] solar installations added, [{numIgnored}] solar installations ignored";
        Logger.Instance.LogInfoEvent(msg);
        return msg;
    }


    public class GeoJson {
        public string type {get; set;}
        public string name {get; set;}
        public Feature[] features {get; set;}
    }

    public class Feature {
        public int id {get; set;}
        public Props properties {get; set;}
        public Geometry geometry { get; set; }
    }

    public class Props {
        public string Name {get; set;}
        public string FolderPath {get; set;}

    }

    public class Geometry {
        public string type {get; set;}
        public double[] coordinates {get; set;}
    }

}