using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;


namespace SmartEnergyLabDataApi.Data
{
    public class Organisations : DataSet
    {
        public Organisations(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }



        #region Distribution network operators
        public void Add(DistributionNetworkOperator dno)
        {
            Session.Save(dno);
        }
        public void Delete(DistributionNetworkOperator dno)
        {
            Session.Delete(dno);
        }

        public DistributionNetworkOperator GetDistributionNetworkOperator(DNOCodes code)
        {
            return Session.QueryOver<DistributionNetworkOperator>().Where(m => m.Code == code ).Take(1).SingleOrDefault();
        }
        #endregion

        #region Geographical areas
        public void Add(GeographicalArea ga)
        {
            Session.Save(ga);
        }
        public void Delete(GeographicalArea ga)
        {
            Session.Delete(ga);
        }

        public GeographicalArea GetGeographicalArea(string name)
        {
            var q = Session.QueryOver<GeographicalArea>().
                        Where(m => m.Name.IsInsensitiveLike(name)).
                        Fetch(SelectMode.Fetch, m=>m.GISData);
            return q.Take(1).SingleOrDefault();
        }

        public void AutoFillGISData(string name) {
            /*var gisFinder = new OpenMapsGISFinder();
            var gasEmptyGis = Session.QueryOver<GeographicalArea>().Where(m=>m.GISData==null).List();
            foreach( var ga in gasEmptyGis) {
                var geometry = gisFinder.LookupCity(ga.Name);
                if ( geometry!=null ) {
                    ga.GISData = new GISData(ga);
                    ga.GISData.Latitude = geometry.Latitude;
                    ga.GISData.Longitude = geometry.Longitude;
                    ga.GISData.BoundaryLatitudes = geometry.PolygonLatitudes;
                    ga.GISData.BoundaryLongitudes = geometry.PolygonLongitudes;
                }
            }*/
            var ga = GetGeographicalArea(name);
            if ( ga!=null) {
                var gsps = DataAccess.SupplyPoints.GetGridSupplyPoints(ga.Id);                
                double east = gsps.Select(m=>m.GISData.Longitude).Min();
                double west = gsps.Select(m=>m.GISData.Longitude).Max();
                double north = gsps.Select(m=>m.GISData.Latitude).Max();
                double south = gsps.Select(m=>m.GISData.Latitude).Min();
                ga.GISData.Longitude = (east+west)/2;
                ga.GISData.Latitude = (north+south)/2;
                ga.GISData.BoundaryLongitudes = new double[] { east,east,west,west };
                ga.GISData.BoundaryLatitudes  = new double[] { south,north,north,south };
            } else {
                throw new Exception($"Could not find geographical are with name [{name}]");
            }

        }

        #endregion
    }


    public class OpenMapsGISFinder
    {
        private HttpClient _client;
        private UriBuilder _builder;

        public OpenMapsGISFinder()
        {
            _client = new HttpClient();
            _builder = new UriBuilder("http://nominatim.openstreetmap.org/search");
        }

        public Geometry LookupCity(string city)
        {
            try {
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["city"] = city;
                query["format"] = "geojson";
                query["polygon_geojson"]="1";
                query["countrycodes"]="gb";
                _builder.Query = query.ToString();
                var url = _builder.ToString();
                _client.DefaultRequestHeaders.Add("Accept","*/*");
                _client.DefaultRequestHeaders.Add("Accept-Encoding","gzip, deflate, br");
                _client.DefaultRequestHeaders.Add("User-Agent","PostmanRuntime/7.29.0");
                var response = _client.GetStringAsync(url).Result;

                var result = JsonSerializer.Deserialize<Geocode>(response);
                var features = result?.features;
                if ( features!=null && features.Count>0) {
                    var geometry = new Geometry();
                    foreach( var feature in features) {
                        if ( feature.geometry.type == "Point") {
                            var coord = feature.geometry.coordinates;
                            if ( coord.Count>0 && coord[0] is JsonElement ) {
                                geometry.Longitude = ((JsonElement) coord[0]).Deserialize<double>();
                                geometry.Latitude = ((JsonElement) coord[1]).Deserialize<double>();
                            }
                        } else if ( feature.geometry.type == "Polygon") {
                            var coord = feature.geometry.coordinates;
                            if ( coord.Count>0 && coord[0] is JsonElement ) {
                                var je = (JsonElement) coord[0];
                                var coords = je.Deserialize<List<double[]>>();
                                geometry.PolygonLatitudes = new double[coords.Count];
                                geometry.PolygonLongitudes = new double[coords.Count];
                                int i =0;
                                foreach( var latLong in coords) {
                                    geometry.PolygonLongitudes[i]=latLong[0];
                                    geometry.PolygonLatitudes[i]=latLong[1];
                                    i++;
                                }
                            }
                        }
                    }
                    return geometry;
                } else {
                    return null;
                }
            } catch( Exception e) {
                Logger.Instance.LogErrorEvent(e.Message);
                throw;
            }
        }

        public class Geocode
        {
            public List<GeocodeResult> features { get; set; }
        }

        public class GeocodeResult
        {
            public RawGeometry geometry { get; set; }
        }

        public class RawGeometry
        {
            public string type { get; set; }
            
            public List<object> coordinates{ get; set; }

        }

        public class Geometry
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public double[] PolygonLatitudes {get ;set; }

            public double[] PolygonLongitudes {get ;set; }
        }

    }

}
