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
                    //var folderPath = feature.properties.FolderPath;
                    //var cpnts= folderPath.Split("/");
                    //var substationName = cpnts[cpnts.Length-1];
                    int dssId = feature.properties.DS_Number!=null ? (int) feature.properties.DS_Number:0;
                    var dssName = feature.properties.DS;
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.NationalGridDistributionOpenData,dssId.ToString(),null,dssName);
                    //
                    if ( dss!=null ) {
                        var si = da.SolarInstallations.GetSolarInstallation(year, dss);
                        if ( si==null ) {                        
                            //
                            si = new SolarInstallation(year, dss, lat, lng);
                            toAdd.Add(si);
                            numNew++;
                        } else {
                            si.GISData.Latitude = lat;
                            si.GISData.Longitude = lng;
                        }
                    } else {
                        numIgnored++;
                        Logger.Instance.LogInfoEvent($"Could not find dist substation [{dssName}] [{dssId}]");
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
        public string DS {get; set;}
        public int? DS_Number {get; set;}

    }

    public class Geometry {
        public string type {get; set;}
        public double[] coordinates {get; set;}
    }

}