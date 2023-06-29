using System.Diagnostics;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models
{
    public class GridSupplyPointLoader
    {
        private DataAccess _da;
        private DistributionNetworkOperator _dno;
        private GeographicalArea _ga;
        private IList<GridSupplyPoint> _gridSupplyPoints;

        public GridSupplyPointLoader(DataAccess da, GeographicalArea ga)
        {
            _da = da;
            _ga = ga;
            _dno = ga.DistributionNetworkOperator;
            _gridSupplyPoints = da.SupplyPoints.GetGridSupplyPoints(_dno);
        }

        public string Load(IFormFile file)
        {
            string msg="";
            using (var stream = file.OpenReadStream()) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                var geoName = geoJson.name;
                int numNew = 0;
                int numModified = 0;
                if ( geoName==null || !geoName.Contains("GSP")) {
                    throw new Exception("Name of geojson file needs to contain \"GSP\"");
                }
                int nSupplyPoints = geoJson.features.Length;
                foreach( var feature in geoJson.features) {                    
                    string nr = feature.properties.NR.ToString();                    
                    var gsp = _gridSupplyPoints.Where( m=>m.NR == nr ).FirstOrDefault();
                    if ( gsp==null ) {
                        gsp = new GridSupplyPoint(ImportSource.File,feature.properties.NAME,feature.properties.NR.ToString(), feature.properties.GSP_NRID.ToString(),_ga,_dno);
                        _da.SupplyPoints.Add(gsp);
                        numNew++;
                    } else {
                        gsp.Name = feature.properties.NAME;
                        numModified++;
                    }
                    
                    //

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
                    var boundary = gsp.GISData.GetFirstBoundary(_da);
                    boundary.Latitudes = new double[length];
                    boundary.Longitudes = new double[length];
                    for(int index=0; index<length; index++) {
                        var eastings = elements[maxIndex][0][index][0];
                        var northings = elements[maxIndex][0][index][1];
                        var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                        boundary.Latitudes[index] = latLong.Latitude;
                        boundary.Longitudes[index] = latLong.Longitude;
                    }
                    //
                    if ( boundary.Latitudes.Length!=0 ) {
                        gsp.GISData.Latitude = boundary.Latitudes.Sum()/boundary.Latitudes.Length;
                    }
                    if ( gsp.GISData.BoundaryLongitudes.Length!=0 ) {
                        gsp.GISData.Longitude = boundary.Longitudes.Sum()/boundary.Longitudes.Length;
                    }
                    //
                    msg = $"[{numNew}] Grid Supply Points added\n[{numModified}] Grid Supply Points modified";
                }
            }
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

        public class Props {
            public int fid {get; set;}
            public int GSP_NRID {get; set;}
            public int BSP_NRID {get; set;}
            public int PRIM_NRID {get; set;}
            public int NR {get; set;}
            public string NAME {get; set;}

        }

        public class Geometry {
            public string type {get; set;}
            public JsonElement coordinates {get; set;}
        }
    }

}