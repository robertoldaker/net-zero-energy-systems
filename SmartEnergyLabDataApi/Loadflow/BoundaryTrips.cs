namespace SmartEnergyLabDataApi.Loadflow
{
    public enum BoundaryTripType { Single, Double }

    public class BoundaryTrips {

        private Dictionary<string,BoundaryTrip> _dict;

        public BoundaryTrips(Branches branches, NodeBoundaryData nbd) {
            Trips = new List<BoundaryTrip>();
            _dict = new Dictionary<string, BoundaryTrip>();
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
                Trips.Add(new BoundaryTrip(index, bw));
                index++;
            }

            // Double trip using combinations of each trip in list
            for(int i=0; i<branchList.Count; i++) {
                for(int j=i+1;j<branchList.Count;j++) {
                    Trips.Add(new BoundaryTrip(index, branchList[i],branchList[j]));
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

        public BoundaryTrip GetTrip(string name) {
            BoundaryTrip trip;
            _dict.TryGetValue(name,out trip);
            return trip;
        }

        public List<BoundaryTrip> Trips {get; private set;}

        public List<string> LineNames {get; private set;}

        public double TotalCapacity {get; private set;}

        public class BoundaryTrip {
            public BoundaryTrip(int index,BranchWrapper bw1) {
                Index = index;
                Type=BoundaryTripType.Single;
                Branch1 = bw1;
                LineNames = new List<string>() {Branch1.LineName};
            }
            public BoundaryTrip(int index,BranchWrapper bw1, BranchWrapper bw2) {
                Index = index;
                Type=BoundaryTripType.Double;
                Branch1 = bw1;
                Branch2 = bw2;
                LineNames = new List<string>() {Branch1.LineName,Branch2.LineName};
            }
            public int Index {get; private set;}
            public BoundaryTripType Type {get; set;}
            public BranchWrapper Branch1;
            public BranchWrapper Branch2;
            public string Text {
                get {
                    string tStr = Type==BoundaryTripType.Single ? "S" : "D";
                    return $"{tStr}{Index}";
                }
            }

            public List<string> LineNames {get; private set;}
        }
    }
}