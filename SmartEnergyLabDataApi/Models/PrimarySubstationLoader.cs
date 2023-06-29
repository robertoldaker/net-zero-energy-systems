using System.Diagnostics;
using System.Text.Json;
using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class PrimarySubstationLoader
    {
        private DataAccess _da;
        private DistributionNetworkOperator _dno;
        private GeographicalArea _ga;
        private IList<PrimarySubstation> _primarySubstations;

        public PrimarySubstationLoader(DataAccess da, GeographicalArea ga)
        {
            _da = da;
            _ga = ga;
            _dno = ga.DistributionNetworkOperator;
            _primarySubstations = da.Substations.GetPrimarySubstations(_dno);
        }

        public string Load(IFormFile file)
        {
            string msg="";
            int numNew = 0;
            int numModified = 0;
            int numIgnored=0;
            using (var stream = file.OpenReadStream()) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nPrimaries = geoJson.features.Length;
                foreach( var feature in geoJson.features) {
                    string nr = feature.properties.NR.ToString();
                    var pss = _primarySubstations.Where( m=>m.NR == nr ).FirstOrDefault();
                    var gsp =  _da.SupplyPoints.GetGridSupplyPointByNRId(feature.properties.GSP_NRID.ToString());
                    if ( gsp==null ) {
                        msg+=$"Could not find GSP with GSP_NRID=[{feature.properties.GSP_NRID}]\n";
                        numIgnored++;
                        continue;
                    }
                    if ( pss==null ) {                        
                        pss = new PrimarySubstation(ImportSource.File,nr,feature.properties.PRIM_NRID.ToString(), gsp);
                        _da.Substations.Add(pss);
                        numNew++;
                    } else {
                        pss.GridSupplyPoint = gsp;
                        numModified++;
                    }
                    //
                    pss.Name = feature.properties.NAME;
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
                    var boundary = pss.GISData.GetFirstBoundary(_da);
                    boundary.Latitudes = new double[length];
                    boundary.Longitudes = new double[length];
                    for(int index=0; index<length; index++) {                            
                        boundary.Longitudes[index] = elements[maxIndex][0][index][0];
                        boundary.Latitudes[index] = elements[maxIndex][0][index][1];
                    }
                    if ( pss.GISData.Latitude==0 && boundary.Latitudes.Length!=0 ) {
                        pss.GISData.Latitude = (boundary.Latitudes.Max()+boundary.Latitudes.Min())/2;
                    }
                    if ( pss.GISData.Longitude==0 && boundary.Longitudes.Length!=0 ) {
                        pss.GISData.Longitude = (boundary.Longitudes.Max()+boundary.Longitudes.Min())/2;
                    }

                }
            }
            msg+=$"[{numNew}] primary substations added\n";
            msg+=$"[{numModified}] primary substations modified\n";
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