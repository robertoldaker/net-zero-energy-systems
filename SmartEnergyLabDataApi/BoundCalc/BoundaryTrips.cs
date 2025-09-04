using Lad.NetworkLibrary;
using NHibernate.Util;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public enum BoundCalcBoundaryTripType { Single, Double, Multi }

    public class BoundCalcBoundaryTrips {

        private Dictionary<string,BoundCalcBoundaryTrip> _dict;

        public BoundCalcBoundaryTrip GetTrip(string name) {
            BoundCalcBoundaryTrip trip;
            _dict.TryGetValue(name,out trip);
            return trip;
        }

        public List<BoundCalcBoundaryTrip> Trips {get; private set;}

        public List<string> LineNames {get; private set;}

        public double TotalCapacity {get; private set;}

        public class BoundCalcBoundaryTrip {

            public BoundCalcBoundaryTrip(BoundCalcNetworkData nd, Network.TripSpec ts)
            {
                _text = ts.Name;
                // Need ids etc. of branch objects so look them up in the NetworkData class
                var branches = nd.GetBranches(ts.ItemNames);
                // Used in the gui
                LineNames = branches.Select(m => m.LineName).ToList<string>();
                BranchCodes = branches.Select(m => m.Code).ToList<string>();
                BranchIds = branches.Select(m => m.Id).ToList<int>();
                // Not sure if intact needs its own type??
                if (ts.ItemNames.Length == 2) {
                    Type = BoundCalcBoundaryTripType.Double;
                } else {
                    Type = BoundCalcBoundaryTripType.Single;
                }
            }

            public int Index { get; private set; }
            public BoundCalcBoundaryTripType Type {get; set;}

            private string _text;
            public string Text {
                get {
                    return _text;
                }
            }

            public List<string> LineNames {get; private set;}
            public List<string> BranchCodes {get; private set;}
            public List<int> BranchIds {get; private set;}
        }
    }
}
