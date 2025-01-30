using System.Text.Json.Serialization;
using NHibernate;
using Org.BouncyCastle.Asn1.Cms;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
     public class Ctrls : DataStore<CtrlWrapper> {
        public Ctrls(DataAccess da, int datasetId, Branches branches)  {
            var q = da.Session.QueryOver<BoundCalcCtrl>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
            q = q.Fetch(SelectMode.Fetch,m=>m.Node2);            
            var di = new DatasetData<BoundCalcCtrl>(da,datasetId,m=>m.Id.ToString(),q);            
            foreach( var c in di.Data) {
                var key = c.LineName;
                var branchWrapper = branches.get(key);
                var branch = branchWrapper;
                var ctrl = new CtrlWrapper(c, branch);
                branch.Ctrl = ctrl;
                base.add(key,ctrl);
            }

            DatasetData = di;

        }
        public DatasetData<BoundCalcCtrl> DatasetData {get; private set;}

        // Base
        public double[]? BaseCVang {get; set; }

        // Boundary xfer
        public double[]? BoundaryCVang {get; set; }

        public List<BoundCalcCtrlResult> GetCtrlResults() {
            var results = Objs.Select(m=>new BoundCalcCtrlResult(m)).ToList();
            return results;
        }
    }

    public class CtrlWrapper : ObjectWrapper<BoundCalcCtrl> {
        public CtrlWrapper(BoundCalcCtrl obj, BranchWrapper branchWrapper) : base(obj) {
            Branch = branchWrapper;            
            if ( obj.Type == BoundCalcCtrlType.QB) {
                InjMax = BoundCalc.PUCONV * Obj.MaxCtrl / branchWrapper.Obj.X;
            } else if ( obj.Type == BoundCalcCtrlType.HVDC) {
                InjMax = Obj.MaxCtrl;
            } else {
                throw new Exception($"Unknown ctrl type found [{obj.Type}]");
            }
        }

        // Branch that it controls (cbid
        public BranchWrapper Branch {get; private set;}

        // Max control injection (injmax)
        public double InjMax {get; private set; }

        // Component Vang 0=base, 1 per max control (fwd direction)
        [JsonIgnore()]
        public double[]? CVang {get; set;} 
        
        // Control set point (csp)
        public double? SetPoint {
            get {
                return Obj.SetPoint;
            }
            set {
                Obj.SetPoint = value;
            }
        }

    }
}