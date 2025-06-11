using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
using SmartEnergyLabDataApi.Loadflow;
using static SmartEnergyLabDataApi.BoundCalc.BoundCalcBoundaryTrips;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class BoundCalcResults {
        public BoundCalcResults(BoundCalc bc, BoundCalcBoundaryFlowResult? bfr=null, BoundCalcBoundaryTrips? bts=null) {

            Dataset = bc.Dataset;
            // stage results
            StageResults = bc.StageResults;
            // Nodes
            Nodes = bc.Nodes.DatasetData;
            // Branches
            Branches = bc.Branches.DatasetData;
            // Controls
            Ctrls = bc.Ctrls.DatasetData;
            if ( bc.SetPointMode==SetPointMode.Auto) {
                foreach( var ct in bc.Ctrls.Objs) {
                    ct.SetPoint = ct.GetSetPoint(bc.SetPointMode);
                }
            }

            // Populate BoundaryTripResults if we ave performed a boundary trip
            if ( bc.WorstTrip!=null ) {
                BoundaryTripResults = new BoundCalcBoundaryTripResults(bc);
            }

            BoundaryFlowResult = bfr;
            BoundaryTrips = bts;

            var misMatches  = bc.Nodes.DatasetData.Data.Where(nw => nw.Mismatch!=null && Math.Abs((double) nw.Mismatch)>0.01).Select(nw => nw.Mismatch).OrderBy(m=>m).ToList();
            NodeMismatchError = misMatches.Count>0;
            if ( NodeMismatchError ) {
                NodeMismatchErrorAsc = Math.Abs((double) misMatches[0]) > Math.Abs((double) misMatches[misMatches.Count-1]);
            }
            BranchCapacityError = bc.Branches.DatasetData.Data.Any(nw => nw.FreePower!=null && nw.FreePower<-1e-2);
            SetPointError = bc.Ctrls.DatasetData.Data.Any(cw => cw.SetPoint!=null && cw.SetPoint>cw.MaxCtrl || cw.SetPoint<cw.MinCtrl);
        }

        public BoundCalcResults(BoundCalcNetworkData nd)
        {
            Dataset = nd.Dataset;
            // stage results
            StageResults = nd.StageResults;
            // Nodes
            Nodes = nd.Nodes;
            // Branches
            Branches = nd.Branches;
            // Controls
            Ctrls = nd.Ctrls;
            if (nd.SetPointMode == SetPointMode.Auto) {
                foreach (var ct in nd.Ctrls.Data) {
                    //??ct.SetPoint = ct.GetSetPoint(nd.SetPointMode);
                }
            }

            // Populate BoundaryTripResults if we have performed a boundary trip
            //??if (nd.WorstTrip != null) {
            //??    BoundaryTripResults = new BoundCalcBoundaryTripResults(bc);
            //??}

            var misMatches = nd.Nodes.Data.Where(n => n.Mismatch != null && Math.Abs((double)n.Mismatch) > 0.01).Select(n => n.Mismatch).OrderBy(m => m).ToList();
            NodeMismatchError = misMatches.Count > 0;
            if (NodeMismatchError) {
                NodeMismatchErrorAsc = Math.Abs((double)misMatches[0]) > Math.Abs((double)misMatches[misMatches.Count - 1]);
            }
            BranchCapacityError = nd.Branches.Data.Any(nw => nw.FreePower != null && nw.FreePower < -1e-2);
            SetPointError = nd.Ctrls.Data.Any(cw => cw.SetPoint != null && cw.SetPoint > cw.MaxCtrl || cw.SetPoint < cw.MinCtrl);
        }

        public BoundCalcResults(string errorMsg)
        {
            StageResults = new BoundCalcStageResults();
            var sr = new BoundCalcStageResult("Error");
            sr.Finish(BoundCalcStageResultEnum.Fail, errorMsg);
            StageResults.Results.Add(sr);
        }

        public Dataset Dataset {get; set;}
        public BoundCalcStageResults StageResults {get; private set;}
        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}
        public DatasetData<Ctrl> Ctrls {get; private set;}

        public BoundCalcBoundaryTripResults BoundaryTripResults {get; private set;}
        public BoundCalcBoundaryFlowResult? BoundaryFlowResult {get; private set;}
        public BoundCalcBoundaryTrips? BoundaryTrips {get; private set;}

        public bool NodeMismatchError {get; set;}
        public bool NodeMismatchErrorAsc {get; set;}
        public bool BranchCapacityError {get; set;}
        public bool SetPointError {get; set;}

        public void Save() {
            using( var da = new DataAccess() ) {
                string json = JsonSerializer.Serialize(this,new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var lfr = da.BoundCalc.GetBoundCalcResult(Dataset.Id);
                if ( lfr==null ) {
                    lfr = new BoundCalcResult(Dataset);
                    da.BoundCalc.Add(lfr);
                }
                lfr.Data = Encoding.UTF8.GetBytes(json);
                da.CommitChanges();
            }
        }

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

    public class BoundCalcCtrlResult {
        public BoundCalcCtrlResult( CtrlWrapper cw, double? sp=null) {
            Id = cw.Obj.Id;
            Code = cw.Obj.Code;
            if ( sp!=null ) {
                SetPoint = sp;
            } else {
                SetPoint = cw.SetPoint;
            }
        }
        public int Id {get; set;}
        public string Code {get; set;}
        public double? SetPoint {get; set;}
    }

    public class BoundCalcBoundaryFlowResult {
        public BoundCalcBoundaryFlowResult(double gin, double din, double gout, double dout, double ia ) {
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

    public class BoundCalcBoundaryTripResults {
        public BoundCalcBoundaryTripResults(BoundCalc bc) {
            IntactTrips = bc.IntactTrips;
            SingleTrips = bc.SingleTrips;
            DoubleTrips = bc.DoubleTrips;
            WorstTrip = new BoundCalcBoundaryTrip(bc.WorstTrip);
        }
        public List<BoundCalcAllTripsResult> IntactTrips {get; private set;}
        public List<BoundCalcAllTripsResult> SingleTrips {get; private set;}
        public List<BoundCalcAllTripsResult> DoubleTrips {get; private set;}

        public BoundCalcBoundaryTrip WorstTrip {get; private set;}

    }

    public class BoundCalcAllTripsResult {

        public double Surplus {get; set;}
        public double Capacity {get; set;}
        public BoundCalcBoundaryTrip Trip {get; set;}
        public IList<string> LimCct {get; set;}
        public IList<BoundCalcCtrlResult> Ctrls {get; set;}

    }


}
