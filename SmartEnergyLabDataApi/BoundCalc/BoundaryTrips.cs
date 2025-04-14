namespace SmartEnergyLabDataApi.BoundCalc
{
    public enum BoundCalcBoundaryTripType { Single, Double, Multi }

    public class BoundCalcBoundaryTrips {

        private Dictionary<string,BoundCalcBoundaryTrip> _dict;

        public BoundCalcBoundaryTrips(Branches branches, NodeBoundaryData nbd) {
            Trips = new List<BoundCalcBoundaryTrip>();
            _dict = new Dictionary<string, BoundCalcBoundaryTrip>();
            var branchList = new List<BranchWrapper>();
            double tc = 0;

            foreach( var bw in branches.Objs) {
                if ( nbd.IsInBoundary(bw.Node1) ^ nbd.IsInBoundary(bw.Node2) ) {
                    branchList.Add(bw);
                    tc+= bw.Obj.Cap;
                }
            }

            // Single trip from the branch list
            int index=1;
            foreach( var bw in branchList) {
                Trips.Add(new BoundCalcBoundaryTrip(index, bw));
                index++;
            }

            // Double trip using combinations of each trip in list
            for(int i=0; i<branchList.Count; i++) {
                for(int j=i+1;j<branchList.Count;j++) {
                    Trips.Add(new BoundCalcBoundaryTrip(index, branchList[i],branchList[j]));
                    index++;
                }
            }

            // list of boundary names
            LineNames = new List<string>();
            foreach( var bw in branchList) {
                LineNames.Add(bw.LineName);
            }

            // Add to dictionary for fast lookup
            foreach( var trip in Trips) {
                _dict.Add(trip.Text,trip);
            }

            TotalCapacity = tc;
        }

        public BoundCalcBoundaryTrip GetTrip(string name) {
            BoundCalcBoundaryTrip trip;
            _dict.TryGetValue(name,out trip);
            return trip;
        }

        public List<BoundCalcBoundaryTrip> Trips {get; private set;}

        public List<string> LineNames {get; private set;}

        public double TotalCapacity {get; private set;}

        public class BoundCalcBoundaryTrip {
            private List<BranchWrapper> _branches;
            public BoundCalcBoundaryTrip(int index,BranchWrapper bw1, BranchWrapper bw2=null) {
                Index = index;
                _branches = new List<BranchWrapper>() { bw1 };
                if ( bw2!=null) {
                    _branches.Add(bw2);
                    Type=BoundCalcBoundaryTripType.Double;
                } else {
                    Type=BoundCalcBoundaryTripType.Single;
                }
                LineNames = _branches.Select(m=>m.LineName).ToList<string>();
                BranchCodes = _branches.Select(m=>m.Obj.Code).ToList<string>();
                BranchIds = _branches.Select(m=>m.Obj.Id).ToList<int>();
            }

            public BoundCalcBoundaryTrip(Trip trip) {
                _text = trip.name;
                _branches = new List<BranchWrapper>(trip.Branches);
                LineNames = _branches.Select(m=>m.LineName).ToList<string>();
                BranchCodes = _branches.Select(m=>m.Obj.Code).ToList<string>();
                BranchIds = _branches.Select(m=>m.Obj.Id).ToList<int>();                
            }
            public int Index {get; private set;}
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