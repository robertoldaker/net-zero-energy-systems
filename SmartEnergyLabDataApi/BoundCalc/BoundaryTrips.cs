namespace SmartEnergyLabDataApi.BoundCalc
{
    public enum BoundCalcBoundaryTripType { Single, Double }

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
            private BranchWrapper _branch1;
            private BranchWrapper _branch2;
            public BoundCalcBoundaryTrip(int index,BranchWrapper bw1) {
                Index = index;
                Type=BoundCalcBoundaryTripType.Single;
                _branch1 = bw1;
                LineNames = new List<string>() {_branch1.LineName};
                BranchIds = new List<int> {_branch1.Obj.Id};
            }
            public BoundCalcBoundaryTrip(int index,BranchWrapper bw1, BranchWrapper bw2) {
                Index = index;
                Type=BoundCalcBoundaryTripType.Double;
                _branch1 = bw1;
                _branch2 = bw2;
                LineNames = new List<string>() {_branch1.LineName,_branch2.LineName};
                BranchIds = new List<int> {_branch1.Obj.Id,_branch2.Obj.Id};
            }
            public int Index {get; private set;}
            public BoundCalcBoundaryTripType Type {get; set;}
            public string Text {
                get {
                    string tStr = Type==BoundCalcBoundaryTripType.Single ? "S" : "D";
                    return $"{tStr}{Index}";
                }
            }

            public List<string> LineNames {get; private set;}
            public List<int> BranchIds {get; private set;}
        }
    }
}