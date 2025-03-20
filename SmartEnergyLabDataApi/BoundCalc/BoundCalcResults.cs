using System.Linq;
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
        public BoundCalcResults(BoundCalc lf, BoundCalcBoundaryFlowResult? bfr=null, BoundCalcBoundaryTrips? bts=null) {

            Dataset = lf.Dataset;
            // stage results
            StageResults = lf.StageResults;
            // Nodes
            Nodes = lf.Nodes.DatasetData;
            // Branches
            Branches = lf.Branches.DatasetData;
            // Controls
            Ctrls = lf.Ctrls.DatasetData;
            if ( lf.setptmode==BoundCalc.SPAuto) {
                foreach( var ct in lf.Ctrls.Objs) {
                    ct.SetPoint = ct.GetSetPoint(lf.setptmode);
                }
            }

            BoundaryFlowResult = bfr;
            BoundaryTrips = bts;

            IntactTrips = lf.IntactTrips;
            SingleTrips = lf.SingleTrips;
            DoubleTrips = lf.DoubleTrips;

            var misMatches  = lf.Nodes.DatasetData.Data.Where(nw => nw.Mismatch!=null && Math.Abs((double) nw.Mismatch)>0.01).Select(nw => nw.Mismatch).OrderBy(m=>m).ToList();
            NodeMismatchError = misMatches.Count>0;
            if ( NodeMismatchError ) {
                NodeMismatchErrorAsc = Math.Abs((double) misMatches[0]) > Math.Abs((double) misMatches[misMatches.Count-1]);
            }
            BranchCapacityError = lf.Branches.DatasetData.Data.Any(nw => nw.FreePower!=null && nw.FreePower<-1e-6);
        }

        public BoundCalcResults(string errorMsg) {
            StageResults = new BoundCalcStageResults();
            var sr = new BoundCalcStageResult("Error");
            sr.Finish(BoundCalcStageResultEnum.Fail,errorMsg);
            StageResults.Results.Add(sr);
        }

        public Dataset Dataset {get; set;}
        public BoundCalcStageResults StageResults {get; private set;}
        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}

        public BoundCalcBoundaryFlowResult? BoundaryFlowResult {get; private set;}
        public BoundCalcBoundaryTrips? BoundaryTrips {get; private set;}

        public List<BoundCalcAllTripsResult> IntactTrips {get; set;}
        public List<BoundCalcAllTripsResult> SingleTrips {get; set;}
        public List<BoundCalcAllTripsResult> DoubleTrips {get; set;}
        public bool NodeMismatchError {get; set;}
        public bool NodeMismatchErrorAsc {get; set;}
        public bool BranchCapacityError {get; set;}

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

    public class BoundCalcAllTripsResult {

        public double Surplus {get; set;}
        public double Capacity {get; set;}
        public BoundCalcBoundaryTrip Trip {get; set;}
        public IList<string> LimCct {get; set;}
        public IList<BoundCalcCtrlResult> Ctrls {get; set;}

    }


}