namespace CommonInterfaces.Models;

public class EVDemandInput {
    private void initialise() {
        predictorParams = new PredictorParams() { vehicleUsage = VehicleUsage.Medium};
        regionData = new List<RegionData>();
    }
    public EVDemandInput() {
        initialise();
    }

    public class Polygon {
        public Polygon() {

        }
        public string className {
            get {
                return "EVDemandInput.Polygon";
            }
        }
        public Polygon(double[] longitudes, double[] latitudes) {
            var minLength = Math.Min(latitudes.Length,longitudes.Length);
            points = new double[minLength][];
            for ( int i=0;i<minLength; i++) {
                points[i]=new double[2] { longitudes[i],latitudes[i]};
            }
        }
        public double[][] points {get; set;}
    }

    /// <summary>
    /// Defines the region over which the prediction will take place
    /// </summary> <summary>
    /// 
    /// </summary>
    public enum RegionType { Dist, Primary, GSP}
    public class RegionData {
        public RegionData() {

        }
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
        public Polygon polygon {get; set;}
        public int numCustomers {get; set;}
    } 
    /// <summary>
    /// Params associated with prediction
    /// </summary>
    public enum VehicleUsage { Low, Medium, High}
    public class PredictorParams {     
        public PredictorParams() {
            years = new List<int>();
        }       
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

