#define LF

using System.ComponentModel;
using System.Text.Json.Serialization;
using NHibernate;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;


namespace SmartEnergyLabDataApi.BoundCalc
{

    public class Branches : DataStore<BranchWrapper> {

        public Branches(DataAccess da, int datasetId, Nodes nodes, bool buildOptimiser) {
            var q = da.Session.QueryOver<BoundCalcBranch>();
            var di = new DatasetData<BoundCalcBranch>(da,datasetId,m=>m.Id.ToString(),q);
            int index=1;
            //?? need to order by Id ??
            var diData = di.Data.OrderBy(m=>m.Id);
            //??foreach( var b in di.Data) {
            foreach( var b in diData) {
                b.Node1 = nodes.DatasetData.GetItem(b.Node1Id);
                b.Node2 = nodes.DatasetData.GetItem(b.Node2Id);
                var key = b.LineName;
                var objWrapper = new BranchWrapper(b,index,nodes, buildOptimiser);
                base.add(key,objWrapper);
                index++;
            }
            DatasetData = di;
        }
        public DatasetData<BoundCalcBranch> DatasetData {get; private set;}

        public bool IsDisconnected(Nodes nodes) {
            var snet = new int[nodes.Count];
            for( int i=0; i<snet.Length;i++ ) {
                snet[i] = i;
            }
            bool chng = false;
            int j1, j2;
            do {
                chng = false;
                foreach( var b in Objs) {
                    var branch = b.Obj;
                    j1 = nodes.getIndex(branch.Node1.Code);
                    j2 = nodes.getIndex(branch.Node2.Code);
                    if ( snet[j1] < snet[j2]) {
                        snet[j2] = snet[j1];
                        chng = true;
                    } else if ( snet[j2] < snet[j1]) {
                        snet[j1] = snet[j2];
                        chng = true;
                    }
                }
            } while (chng);
            //
            var nonZeros = snet.Where(m=>m!=0).ToList();
            return nonZeros.Count()>0;
        }

        public void ResetOutaged() {
            foreach( var bw in _list) {
                bw.Outaged = false;
            }
        }


    }

    public class BranchWrapper : ObjectWrapper<BoundCalcBranch> {
        private static Dictionary<string,Dictionary<int,double>> _kmScalingOHL = new Dictionary<string, Dictionary<int, double>>() 
        {
            {
                "NGC",new Dictionary<int, double>() 
                { 
                    {400, 1 },
                    {275, 1.2},
                    {132, 2.87}
                }
            },
            {
                "SP",new Dictionary<int, double>() 
                { 
                    {400, 1 },
                    {275, 1.2},
                    {132, 2.87}
                }
            },            
            {
                "SSE",new Dictionary<int, double>() 
                { 
                    {400, 1 },
                    {275, 1.2},
                    {132, 2.59}
                }
            },            
        };

        private Dictionary<string,Dictionary<int,double>> _kmScalingCable = new Dictionary<string, Dictionary<int, double>>() 
        {
            {
                "NGC",new Dictionary<int, double>() 
                { 
                    {400, 10.2 },
                    {275, 11.45},
                    {132, 22.58}
                }
            },
            {
                "SP",new Dictionary<int, double>() 
                { 
                    {400, 10.2 },
                    {275, 11.45},
                    {132, 22.58}
                }
            },            
            {
                "SSE",new Dictionary<int, double>() 
                { 
                    {400, 10.2 },
                    {275, 11.45},
                    {132, 22.77}
                }
            },            
        };

        public BranchWrapper(BoundCalcBranch obj, int index, Nodes nodes, bool buildOptimiser) : base(obj,index) {
            Node1 = nodes.get(obj.Node1.Code);
            Node2 = nodes.get(obj.Node2.Code);
            if ( buildOptimiser ) {
                setKm();
                if ( Obj.X!=0) {
                    y = BoundCalc.PUCONV / Obj.X;
                }
            }
        }

        private void setKm() {
            // non transformer with non-zero cable or OHL length
            if ( Node1.Obj.Voltage == Node2.Obj.Voltage && (Obj.OHL>0 || Obj.CableLength>0) ) {
                double scalingOHL, scalingCable;
                Dictionary<int,double> voltageDict;
                if ( _kmScalingOHL.TryGetValue(Obj.Region,out voltageDict)) {
                    if ( !voltageDict.TryGetValue(Node1.Obj.Voltage, out  scalingOHL)) {
                        throw new Exception($"Cannot find OHL scaling factor for node [{Node1.Obj.Name}], voltage [{Node1.Obj.Voltage}]");
                    }
                } else {
                    throw new Exception($"Cannot find OHL scaling factor for branch [{Obj.Code}], region [{Obj.Region}]");
                }
                if ( _kmScalingCable.TryGetValue(Obj.Region,out voltageDict)) {
                    if ( !voltageDict.TryGetValue(Node1.Obj.Voltage, out scalingCable)) {
                        throw new Exception($"Cannot find OHL scaling factor for node [{Node1.Obj.Name}], voltage [{Node1.Obj.Voltage}]");
                    }
                } else {
                    throw new Exception($"Cannot find OHL scaling factor for branch [{Obj.Code}], region [{Obj.Region}]");
                }
                if ( Obj.Code == "C1AD") {
                    Console.WriteLine("!!");
                }
                km = Obj.OHL * scalingOHL + Obj.CableLength * scalingCable;
            } 
        }

        // Line code name (lcstr)
        public string LineName {
            get {
                return Obj.LineName;
            }
        }

        [JsonIgnore()]
        // Node 1 wrapper
        public NodeWrapper Node1 {get; set;}

        // Node 2 wrapper
        [JsonIgnore()]
        public NodeWrapper Node2 {get; set;}

        // Node 1 order position (bn1)
        public int Node1Index {get; set;}

        // Node 2 order position (bn2)
        public int Node2Index {get; set;}

        // Control associated with branch (bctrl)
        [JsonIgnore()]
        public CtrlWrapper Ctrl {get; set;}

        // outaged (bout)
        public bool Outaged {
                get {
                    return Obj.Outaged;
                }
                set {
                    Obj.Outaged = value;
                }
        }

        // Intact planned flow (ipflow)
        public double? PowerFlow {
            get {
                return Obj.PowerFlow;
            }
            set {
                Obj.PowerFlow = value;
            }
        }

        // Boundary flow
        public double? BFlow {
            get {
                return Obj.BFlow;
            }
            set {
                Obj.BFlow = value;
            }
        }

        // Free power
        public double? FreePower {
            get {
                return Obj.FreePower;
            }
            set {
                Obj.FreePower = value;
            }
        }

        public int pn1 {get; set;} // Row/column position of node1 and node2 in admittance matrix

        public int pn2 {get; set;}

        public double? km { 
            get {
                return Obj.km;
            }
            set {
                Obj.km = value;
            }
        }

        public double y; // admittance in 1MVA base

        public bool BOut {get; set;} = false;

        public double flow(double[] vang, int setptmd, bool outages) {
            double f;
            if ( BOut && outages) {
                f = 0;
#if LF
            } else if ( this.Ctrl == null ) {
                f = (vang[pn1] - vang[pn2]) * y; // uncontrolled ac branch
            } else {
                switch( this.Ctrl.Obj.Type) {
                    case BoundCalcCtrlType.QB:
                        f = (vang[pn1] - vang[pn2] + this.Ctrl.GetSetPoint(setptmd))*y;
                        break;
                    case BoundCalcCtrlType.HVDC:
                        f = this.Ctrl.GetSetPoint(setptmd);
                        break;
                    default:
                        throw new Exception($"Unknown control type {this.Ctrl.Obj.Type}");
                }
            }
#else
            } else if ( this.Obj.X !=0 ) {
                f = (vang[pn1] - vang[pn2]) * y;
            } else {
                f =0;
            }
#endif
            return f;
        }

        // calculate flow direction given vang
        public double Dirn(double[] vang) {
            if ( vang[pn1] >= vang[pn2] ) {
                return 1;
            } else {
                return -1;
            }
        }
    }
}