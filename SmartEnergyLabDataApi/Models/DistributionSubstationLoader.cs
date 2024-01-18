using System.Diagnostics;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models
{
    public class DistributionSubstationLoader
    {
        private DataAccess _da;
        public DistributionSubstationLoader(DataAccess da)
        {
            _da = da;
        }

        public string Load(IFormFile file)
        {
            string msg="";
            int numNew = 0;
            int numModified = 0;
            int numIgnored=0;
            var toAdd = new List<DistributionSubstation>();
            using (var stream = file.OpenReadStream()) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nDss = geoJson.features.Length;
                int nDone=0;
                Console.WriteLine($"Number of substations=[{nDss}]");
                foreach( var feature in geoJson.features) {
                    string nr = feature.properties.NR.ToString();
                    Console.WriteLine($"Processing {nDone++} of {nDss}, [{feature.properties.NAME}] ");
                    var dss = _da.Substations.GetDistributionSubstationByNr(nr);
                    var pss = _da.Substations.GetPrimarySubstation(feature.properties.primary_NR.ToString());
                    if ( pss==null ) {
                        msg+=$"Could not find Primary substation with PRIM_NRID=[{feature.properties.PRIM_NRID}]\n";
                        numIgnored++;
                        continue;
                    }
                    if ( dss==null ) {                        
                        dss = new DistributionSubstation(ImportSource.File,nr,null,pss);
                        toAdd.Add(dss);
                        numNew++;
                    } else {
                        dss.PrimarySubstation = pss;
                        numModified++;
                    }
                    //
                    dss.Name = feature.properties.NAME;
                    // location
                    var eastings = feature.properties.dp2_x;                    
                    var northings = feature.properties.dp2_y;
                    var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                    dss.GISData.Latitude = latLong.Latitude;
                    dss.GISData.Longitude = latLong.Longitude;
                    // boundary
                    var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                    int maxIndex=0;
                    int maxLength = 0;
                    for(int i=0;i<elements.Length;i++) {
                        if ( elements[i][0].Length>maxLength) {
                            maxIndex=i;
                            maxLength = elements[i][0].Length;
                        }
                    }
                    var length = elements[maxIndex][0].Length;
                    var boundary = dss.GISData.GetFirstBoundary(_da);
                    boundary.Latitudes = new double[length];
                    boundary.Longitudes = new double[length];
                    for(int index=0; index<length; index++) {                            
                        latLong=LatLonConversions.ConvertOSToLatLon(elements[maxIndex][0][index][0],elements[maxIndex][0][index][1]);
                        boundary.Longitudes[index] = latLong.Longitude;
                        boundary.Latitudes[index] = latLong.Latitude;
                    }
                }

                // Add new ones to db
                foreach( var dss in toAdd) {
                    _da.Substations.Add(dss);
                }
            }

            msg+=$"[{numNew}] distribution substations added\n";
            msg+=$"[{numModified}] distibutions substations modified\n";
            return msg;
        }

        public void Print(IFormFile file)
        {
            using (var stream = file.OpenReadStream()) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nPrimaries = geoJson.features.Length;
                Console.WriteLine($"Features={nPrimaries}");
                foreach( var feature in geoJson.features) {
                    var p = feature.properties;
                    Console.WriteLine($"{p.NAME}\t{p.BSP_NRID}\t{p.GSP_NRID}\t{p.PRIM_NRID}");
                }
            }
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

        //{ "type": "Feature", "properties": { "NRID": 4298, "NR": 332424, "NR_TYPE_ID": 19, "NAME": "Drakes Island", "GSP_NRID": 139, "BSP_NRID": 31, "PRIM_NRID": 246, "STATUS": 1030, "dp2_x": 246885.966, "dp2_y": 52859.523, "primary_NR": 330015, "primary_NAME": "Newport Street" }, "geometry": { "type": "MultiPolygon", "coordinates": [ [ [ [ 246906.0, 52647.0 ], [ 246821.0, 52784.0 ], [ 246684.0, 52879.0 ], [ 246737.0, 52931.0 ], [ 247043.0, 52858.0 ], [ 247095.0, 52700.0 ], [ 246906.0, 52647.0 ] ] ] ] } },


        public class Props {
            public int NRID {get; set;}
            public int NR {get; set;}
            public string NAME {get; set;}
            public int GSP_NRID {get; set;}
            public int BSP_NRID {get; set;}
            public int PRIM_NRID {get; set;}
            public double dp2_x {get; set;}
            public double dp2_y {get; set;}
            public int primary_NR {get; set;}

        }

        public class Geometry {
            public string type {get; set;}
            public JsonElement coordinates {get; set;}
        }
    }

}