using System.Text.Json;
using System.Web;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class OpenChargeMapLoader {

        private DataAccess _da;
        private HttpClient _client;
        private UriBuilder _builder;
        private VehicleChargingCache _cache;
        private IList<PrimarySubstation> _primarySubstations;
        private GeographicalArea _ga;

        public OpenChargeMapLoader(DataAccess da, GeographicalArea ga) {            
            _da = da;
            _ga = ga;
            _client = new HttpClient();
            var key = "4633edb6-5995-4b31-9a7d-e55a14f2302b";
            _client.DefaultRequestHeaders.Add("X-API-Key",key);
            _builder = new UriBuilder("https://api.openchargemap.io/v3/poi");
            _cache = new VehicleChargingCache(da, ga);
            _primarySubstations = da.Substations.GetPrimarySubstations(ga.DistributionNetworkOperator);
        }

        public void Load(double lat, double lng, double radiusInKm) {
            Logger.Instance.LogInfoEvent($"Starting load of charging stations for [{_ga.Name}] using OpenChargeMap");
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["countrycode"] = "gb";
            query["latitude"] = lat.ToString();
            query["longitude"] = lng.ToString();
            query["distance"] = radiusInKm.ToString();
            query["distanceunit"] = "km";
            query["maxresults"] = "100";
            query["minpowerkw"] = "0";
            query["camelcase"] = "true";

            _builder.Query = query.ToString();
            var url = _builder.ToString();
            var response = _client.GetStringAsync(url).Result;

            var results = JsonSerializer.Deserialize<POI[]>(response);
            if ( results!=null) {
                processResults(results);
            }
            Logger.Instance.LogInfoEvent($"Finished load of charging stations for [{_ga.Name}]");
        }

        private void processResults(POI[] pois) {
            foreach( var poi in pois) {
                var vcs = _cache.GetVehicleChargingStation(poi.id.ToString());
                // Not found then add it as it is a new charging station
                if ( vcs == null ) {
                    PrimarySubstation pss = null;
                    if ( poi.addressInfo.latitude!=null && poi.addressInfo.longitude!=null ) {
                        pss = getPrimarySubstation((double) poi.addressInfo.latitude,(double) poi.addressInfo.longitude);
                    }
                    if ( pss==null ) {
                        Logger.Instance.LogInfoEvent($"Ignoring charging station [{poi.addressInfo.title}] [${poi.addressInfo.latitude},${poi.addressInfo.longitude}] since we cannot find an enclosing primary substation");
                        continue;
                    }
                    vcs = new VehicleChargingStation(poi.id.ToString(), pss);
                    vcs.Source = VehicleChargingStationSource.OpenChargeMap;
                    _cache.Add(vcs);
                    _da.VehicleCharging.Add(vcs);
                    Logger.Instance.LogInfoEvent($"Added new charging station [{poi.addressInfo.title}]");
                } 
                //?? can be removed since this may need to set manually??
                // update primary substation
                if ( poi.addressInfo.latitude!=null && poi.addressInfo.longitude!=null ) {
                    var pss = getPrimarySubstation((double) poi.addressInfo.latitude,(double) poi.addressInfo.longitude);
                    if ( pss==null ) {
                        pss = _primarySubstations[0];
                    }
                    vcs.PrimarySubstation = pss;
                }
                //
                vcs.Name = poi.addressInfo.title;
                //
                if ( poi.addressInfo.latitude!=null && poi.addressInfo.longitude!=null) {
                    vcs.GISData.Latitude = (double) poi.addressInfo.latitude;
                    vcs.GISData.Longitude = (double) poi.addressInfo.longitude;
                }
                // Delete any existing connection that do not now exist
                foreach( var con in vcs.Connections) {
                    if ( poi.connections.Where(m=>m.id.ToString()==con.ExternalId).FirstOrDefault()==null) {
                        _da.VehicleCharging.Delete(con);
                    }
                }
                //
                foreach( var con in poi.connections) {
                    var externalId = con.id.ToString();
                    VehicleChargingConnection vcc = vcs.Connections.Where(m=>m.ExternalId == externalId).FirstOrDefault();                    
                    if (vcc==null) {
                        vcc = new VehicleChargingConnection(externalId, vcs);                        
                        _da.VehicleCharging.Add(vcc);
                    }
                    //
                    if ( con.amps!=null ){
                        vcc.Current = (double) con.amps;
                    }
                    //
                    if ( con.voltage!=null ) {
                        vcc.Voltage = (double) con.voltage;
                    }
                    //
                    if ( con.powerKW!=null ) {
                        vcc.PowerKw = (double) con.powerKW;
                    }
                    //
                    vcc.Quantity = con.quantity;
                    //
                    // Connection type
                    var conType = _cache.GetVehicleChargingConnectionType(con.connectionTypeID.ToString());
                    if( conType == null) {
                        conType = new VehicleChargingConnectionType(con.connectionTypeID.ToString());
                        _da.VehicleCharging.Add(conType);
                        _cache.Add(conType);
                        Logger.Instance.LogInfoEvent($"Added new charging connection type [{con.connectionType.title}]");
                    }
                    vcc.ConnectionType = conType;
                    conType.FormalName = con.connectionType.formalName;
                    conType.Name = con.connectionType.title;

                    // Current type
                    var curType = _cache.GetVehicleChargingCurrentType(con.currentTypeID.ToString());
                    if( curType == null) {
                        curType = new VehicleChargingCurrentType(con.currentTypeID.ToString());
                        _da.VehicleCharging.Add(curType);
                        _cache.Add(curType);
                        Logger.Instance.LogInfoEvent($"Added new charging current type [{con.currentType.title}]");
                    }
                    vcc.CurrentType = curType;
                    curType.Description = con.currentType.description;
                    curType.Name = con.currentType.title;

                }
            }
        }

        private PrimarySubstation getPrimarySubstation(double lat, double lng) {
            foreach( var pss in _primarySubstations) {
                if( pss.GISData.BoundaryLatitudes!=null && pss.GISData.BoundaryLongitudes!=null) {
                    var pointIn = GISUtilities.IsPointInPolygon(lat,lng,pss.GISData.BoundaryLatitudes,pss.GISData.BoundaryLongitudes);
                    if ( pointIn ) {
                        return pss;
                    }
                }
            }
            return null;
        }

        public class POI {
            public OperatorInfo operatorInfo {get; set;}
            public int id {get; set;}
            public AddressInfo addressInfo {get; set;}
            public Connection[] connections {get; set;}
        }

        public class OperatorInfo {
            public string title {get; set;}

        }

        public class AddressInfo {
            public string title {get; set;}
            public double? latitude {get; set;}
            public double? longitude {get; set;}
        }

        public class Connection {

            public int id {get; set;}

            public int connectionTypeID {get; set; }

            public ConnectionType connectionType {get; set;}

            public double? amps {get; set;}

            public double? voltage {get; set;}

            public double? powerKW {get; set;}

            public int currentTypeID {get; set; }

            public CurrentType currentType {get; set;}
            public int quantity {get; set;} 

        }

        public class ConnectionType {
            public string formalName {get; set;}
            public string title {get; set;}
        }

        public class CurrentType {
            public string description {get; set;}
            public string title {get; set;}
        }

    }

}