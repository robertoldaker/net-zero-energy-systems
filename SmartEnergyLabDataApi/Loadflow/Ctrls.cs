using System.Text.Json.Serialization;
using NHibernate;
using Org.BouncyCastle.Asn1.Cms;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Loadflow
{
     public class Ctrls : DataStore<CtrlWrapper> {
        public Ctrls(DataAccess da, int datasetId, Branches branches)  {
            var q = da.Session.QueryOver<Ctrl>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
            q = q.Fetch(SelectMode.Fetch,m=>m.Node2);            
            var di = new DatasetData<Ctrl>(da,datasetId,m=>m.Id.ToString(),q);            
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
        public DatasetData<Ctrl> DatasetData {get; private set;}

        // Base
        public double[]? BaseCVang {get; set; }

        // Boundary xfer
        public double[]? BoundaryCVang {get; set; }

        public List<CtrlResult> GetCtrlResults() {
            var results = Objs.Select(m=>new CtrlResult(m)).ToList();
            return results;
        }
    }

    public class CtrlWrapper : ObjectWrapper<Ctrl> {
        public CtrlWrapper(Ctrl obj, BranchWrapper branchWrapper) : base(obj) {
            Branch = branchWrapper;            
            if ( obj.Type == LoadflowCtrlType.QB) {
                InjMax = Loadflow.PUCONV * Obj.MaxCtrl / branchWrapper.Obj.X;
            } else if ( obj.Type == LoadflowCtrlType.HVDC) {
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