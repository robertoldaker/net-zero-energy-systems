using System.Linq;
using static SmartEnergyLabDataApi.Loadflow.BoundaryTrips;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowResults {
        public LoadflowResults(Loadflow lf, BoundaryFlowResult? bfr=null, BoundaryTrips? bts=null) {
            StageResults = lf.StageResults;
            // Nodes
            Nodes = lf.Nodes.Objs;
            // Branches
            Branches = lf.Branches.Objs;
            // Controls
            Ctrls = lf.Ctrls.Objs;
            
            BoundaryFlowResult = bfr;
            BoundaryTrips = bts;
        }

        public StageResults StageResults {get; private set;}

        public IList<NodeWrapper> Nodes {get; private set;}
        public IList<BranchWrapper> Branches {get; private set;}        
        public IList<CtrlWrapper> Ctrls {get; private set;}

        public BoundaryFlowResult? BoundaryFlowResult {get; private set;}
        public BoundaryTrips? BoundaryTrips {get; private set;}

        public List<AllTripsResult> SingleTrips {get; set;}
        public List<AllTripsResult> DoubleTrips {get; set;}

    }

    public class NodeResult {
        public NodeResult(NodeWrapper nw) {
            Id = nw.Obj.Id;
            Mismatch = nw.Mismatch;
            Code = nw.Obj.Code;
        }
        public int Id {get; set;} 
        public string Code {get; set;}
        public double? Mismatch {get; set;}

    }

    public class BranchResult {
        public BranchResult(BranchWrapper bw) {
            Id = bw.Obj.Id;
            Code = bw.Obj.Code;
            PowerFlow = bw.PowerFlow;
            FreePower = bw.FreePower;
        }
        public int Id {get; set;}
        public string Code {get; set;}
        public double? PowerFlow {get; set;}
        public double? FreePower {get; set;}
    }

    public class CtrlResult {
        public CtrlResult( CtrlWrapper cw) {
            Id = cw.Obj.Id;
            Code = cw.Obj.Code;
            SetPoint = cw.SetPoint;
        }
        public int Id {get; set;}
        public string Code {get; set;}
        public double? SetPoint {get; set;}
    }

    public class BoundaryFlowResult {
        public BoundaryFlowResult(double gin, double din, double gout, double dout, double ia ) {
            GenInside = gin;
            DemInside = din;
            GenOutside = gout;
            DemOutside = dout;
            IA = ia;
        }
        public double GenInside {get; private set;} 
        public double DemInside {get; private set;}
        public double GenOutside {get; private set;}
        public double DemOutside {get; private set;}

        public double IA {get; private set;}
    }

    public class AllTripsResult {
        public double Surplus {get; set;}
        public double Capacity {get; set;}
        public BoundaryTrip Trip {get; set;}
        public IList<string> LimCct {get; set;}
        public IList<CtrlResult> Ctrls {get; set;}

    }
}