using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaloSoft.EventLogger;
using Npgsql.Replication;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{    
    public class EVDemandRunner {
        private static EVDemandRunner? _instance=null;
        public static void Initialise() {
            _instance = new EVDemandRunner();
        }

        public static EVDemandRunner Instance {
            get{
                if ( _instance==null) {
                    throw new Exception("Please run EVDemandRunner.Initialise() before accessing instance member");
                }
                return _instance;
            }
        }

        private EVDemandRunner() {

        }

        public void RunDistributionSubstation(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromDistributionId(id);
            var inputStr=JsonSerializer.Serialize(input);
            Console.WriteLine(inputStr);
        }

        public void RunPrimarySubstation(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromPrimaryId(id);
            var inputStr=JsonSerializer.Serialize(input);
        }

        public void RunGridSupplyPoint(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromGridSupplyPointId(id);
            var inputStr=JsonSerializer.Serialize(input);
        }


    }

    public class EVDemandInput {
        private void initialise() {
            predictorParams = new PredictorParams() { vehicleUsage = VehicleUsage.Medium};
            regionData = new List<RegionData>();
        }
        public EVDemandInput() {
            initialise();
        }

        public static EVDemandInput CreateFromDistributionId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var dss = da.Substations.GetDistributionSubstation(id);
                if ( dss==null) {
                    throw new Exception($"Could not find substation with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(dss.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var boundary=boundaries[0];
                    var rD = new RegionData(id,RegionType.Dist);
                    rD.latitudes = boundary.Latitudes;
                    rD.longitudes = boundary.Longitudes;
                    if ( dss.SubstationData!=null ) {
                        rD.numCustomers = dss.SubstationData.NumCustomers;
                    } else {
                        throw new Exception($"No substation data defined for dss=[{dss.Name}]");
                    }
                    evDi.regionData.Add(rD);
                } else {
                    throw new Exception($"No boundaries defined for dss=[{dss.Name}]");
                }
            }
            return evDi;
        }

        public static EVDemandInput CreateFromPrimaryId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var pss = da.Substations.GetPrimarySubstation(id);
                if ( pss==null) {
                    throw new Exception($"Could not find substation with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(pss.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var boundary=boundaries[0];
                    var rD = new RegionData(id,RegionType.Primary);
                    rD.latitudes = boundary.Latitudes;
                    rD.longitudes = boundary.Longitudes;
                    var numCustomers = da.Substations.GetCustomersForPrimarySubstation(id);
                    if ( numCustomers>0 ) {
                        rD.numCustomers = numCustomers;
                    } else {
                        throw new Exception($"Num customers is 0 for pss=[{pss.Name}]");
                    }
                    evDi.regionData.Add(rD);
                } else {
                    throw new Exception($"No boundaries defined for pss=[{pss.Name}]");
                }
            }
            return evDi;
        }

        public static EVDemandInput CreateFromGridSupplyPointId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var gsp = da.SupplyPoints.GetGridSupplyPoint(id);
                if ( gsp==null) {
                    throw new Exception($"Could not find grid supply point with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(gsp.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var psss=da.Substations.GetPrimarySubstationsByGridSupplyPointId(id);
                    foreach( var boundary in boundaries) {
                        var numCustomers=0;
                        foreach( var pss in psss ) {
                            var lat = pss.GISData.Latitude;
                            var lng = pss.GISData.Longitude;
                            if( GISUtilities.IsPointInPolygon(lat,lng, boundary.Latitudes,boundary.Longitudes )) {
                                numCustomers += da.Substations.GetCustomersForPrimarySubstation(pss.Id);
                            }
                        }
                        if ( numCustomers>0 ) {
                            var rD = new RegionData(id,RegionType.GSP);
                            rD.latitudes = boundary.Latitudes;
                            rD.longitudes = boundary.Longitudes;
                            rD.numCustomers = numCustomers;
                            evDi.regionData.Add(rD);
                        } 
                    }
                } else {
                    throw new Exception($"No boundaries defined for gsp=[{gsp.Name}]");
                }
            }
            return evDi;
        }

        /// <summary>
        /// Defines the region over which the prediction will take place
        /// </summary> <summary>
        /// 
        /// </summary>
        public enum RegionType { Dist, Primary, GSP}
        public class RegionData {
            public RegionData(int _id, RegionType _type) {
                id = _id;
                type=_type;
            }
            public string className {
                get {
                    return "EVDemandInput.RegionData";
                }
            }
            public int id {get; set;}
            public RegionType type{ get; set;}
            public double[] latitudes {get; set;}
            public double[] longitudes {get; set;}
            public int numCustomers {get; set;}
        } 
        /// <summary>
        /// Params associated with prediction
        /// </summary>
        public enum VehicleUsage { Low, Medium, High}
        public class PredictorParams {            
            public string className {
                get {
                    return "EVDemandInput.PredictorParams";
                }
            }
            public VehicleUsage vehicleUsage {get; set;}

            //?? Not used at present - but could be??
            public List<int> years {get; set;}
        } 

        public string className {
            get {
                return "EVDemandInput";
            }
        }
        public List<RegionData> regionData {get; set;}
        public PredictorParams predictorParams {get;set;}
    }

    
}
