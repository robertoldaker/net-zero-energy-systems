using System.Net;
using System.Text.Json.Serialization;
using NHibernate;
using Org.BouncyCastle.Asn1.Cms;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc
{
     public class Ctrls : DataStore<CtrlWrapper> {
        public Ctrls(DataAccess da, int datasetId, BoundCalc boundCalc)  {
            var q = da.Session.QueryOver<Ctrl>();
            q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
            q = q.Fetch(SelectMode.Fetch,m=>m.Node2);            
            var di = new DatasetData<Ctrl>(da,datasetId,m=>m.Id.ToString(),q);
            int index=1;
            foreach( var c in di.Data) {
                var key = c.LineName;
                var branchWrapper = boundCalc.Branches.get(key);
                var branch = branchWrapper;
                var ctrl = new CtrlWrapper(c, index, branch, boundCalc);
                branch.Ctrl = ctrl;
                base.add(key,ctrl);
                index++;
            }

            DatasetData = di;

        }
        public DatasetData<Ctrl> DatasetData {get; private set;}

        // Base
        public double[]? BaseCVang {get; set; }

        // Boundary xfer
        public double[]? BoundaryCVang {get; set; }

        public List<BoundCalcCtrlResult> GetCtrlResults() {
            var results = Objs.Select(m=>new BoundCalcCtrlResult(m)).ToList();
            return results;
        }
    }

    public class CtrlWrapper : ObjectWrapper<Ctrl> {
        public CtrlWrapper(Ctrl obj, int index, BranchWrapper branchWrapper, BoundCalc boundCalc) : base(obj, index) {
            Branch = branchWrapper;            
            BoundCalc = boundCalc;
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

        public BoundCalc BoundCalc {get; set;}

        public LPVarDef CtVar {get; set;}

        public double GetSetPoint(SetPointMode spt) {
            double v = this.Obj.MaxCtrl / InjMax;
            switch( spt) {
                case SetPointMode.Auto:
                    return v * CtVar.Value(BoundCalc.Optimiser.ctrllp);
                case SetPointMode.Manual:
                    return (double) Obj.SetPoint;
                case SetPointMode.Zero:
                    return 0;
                default:
                    throw new Exception($"Unknown setpoint type request [{spt}]");
            }
        }

    }
}