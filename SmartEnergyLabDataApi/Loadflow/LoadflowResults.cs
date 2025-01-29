using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.Loadflow;
using static SmartEnergyLabDataApi.Loadflow.BoundaryTrips;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class LoadflowResults {
        public LoadflowResults(Loadflow lf, BoundaryFlowResult? bfr=null, BoundaryTrips? bts=null) {

            Dataset = lf.Dataset;
            // stage results
            StageResults = lf.StageResults;
            // Nodes
            Nodes = lf.Nodes.DatasetData;
            // Branches
            Branches = lf.Branches.DatasetData;
            // Controls
            Ctrls = lf.Ctrls.DatasetData;
            
            BoundaryFlowResult = bfr;
            BoundaryTrips = bts;

        }

        public Dataset Dataset {get; set;}
        public StageResults StageResults {get; private set;}
        public DatasetData<Node> Nodes {get; private set;}
        public DatasetData<Branch> Branches {get; private set;}        
        public DatasetData<Ctrl> Ctrls {get; private set;}

        public BoundaryFlowResult? BoundaryFlowResult {get; private set;}
        public BoundaryTrips? BoundaryTrips {get; private set;}

        public List<AllTripsResult> SingleTrips {get; set;}
        public List<AllTripsResult> DoubleTrips {get; set;}

        public void Save() {
            using( var da = new DataAccess() ) {
                string json = JsonSerializer.Serialize(this,new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });                
                var lfr = da.Loadflow.GetLoadflowResult(Dataset.Id);
                if ( lfr==null ) {
                    lfr = new LoadflowResult(Dataset);
                    da.Loadflow.Add(lfr);
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