using NHibernate.Util;
using Renci.SshNet.Security;
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;

namespace SmartEnergyLabDataApi.BoundCalc;

public class Boundaries : DataStore<BoundaryWrapper> {

    public Boundaries(DataAccess da, int datasetId, BoundCalc boundCalc) {
        var q = da.Session.QueryOver<Boundary>();
        var ds = new DatasetData<Boundary>(da, datasetId,m=>m.Id.ToString(),q);
        // add zones they belong to
        var boundDict = da.BoundCalc.GetBoundaryZoneDict(ds.Data);
        int index=1;
        foreach( var b in ds.Data) {
            if ( boundDict.ContainsKey(b) ) {
                b.Zones = boundDict[b];
            } else {
                b.Zones = new List<Zone>();
            }
            var objWrapper = new BoundaryWrapper(b,index, boundCalc);
            base.add(b.Code,objWrapper);
            index++;
        }
        DatasetData = ds;
    }
    public DatasetData<Boundary> DatasetData {get; private set;}

    public BoundaryWrapper? GetBoundary(string boundaryName) {
        return this.Objs.Where( m=>m.Obj.Code == boundaryName).FirstOrDefault();
    }

}

public class BoundaryWrapper : ObjectWrapper<Boundary> {

    public string name {
        get {
            return Obj.Code;
        }
    }
    public string trips {get; set;}

    public Collection<Trip> STripList {get; private set;}
    public Collection<Trip> DTripList {get; private set;}

    private double _genIn = 0; // Scaleable generation inside boundary
    private double _genInUS = 0; // Unscalable generaion inside boundary
    private double _genOut = 0;
    private double _genOutUS = 0;
    private double _demIn = 0;
    private double _demInUS = 0;
    private double _demOut = 0;
    private double _demOutUS = 0;
    public double PlannedTransfer {get; private set;}
    public double InterconAllowance {get; private set;}
    private double kgin; // inside generation scaling for interconnection allowance
    private double kdin;
    private double kgout;
    private double kdout;
    public Collection<BranchWrapper> BoundCcts {get; private set;}

    public BoundaryWrapper(Boundary b, int index, BoundCalc boundCalc) : base(b, index) {
        foreach( var z in boundCalc.Zones.Data ) {
            if ( b.Zones.FirstOrDefault(m=>m.Id == z.Id)!=null ) {
                _genIn += z.TGeneration;
                _genInUS += z.UnscaleGen;
                _demIn += z.Tdemand;
                _demInUS += z.UnscaleDem;
            } else {
                _genOut += z.TGeneration;
                _genOutUS += z.UnscaleGen;
                _demOut += z.Tdemand;
                _demOutUS += z.UnscaleDem;
            }
        }
        PlannedTransfer = _genIn + _genInUS - _demIn - _demInUS;
        InterconAllowance = InterconnectionAllowance(_genIn + _genInUS, _demIn + _demInUS, _demIn + _demInUS + _demOut + _demOutUS);

        if ( PlannedTransfer < 0) {
            InterconAllowance = -InterconAllowance; // Interconnection allowance increases magnitude of planned transfer
        }

        kgin = InterconAllowance / ( _genIn + _demIn);
        kdin = - kgin;
        kdout = InterconAllowance / ( _genOut + _demOut);
        kgout = -kdout;

        BoundCcts = new Collection<BranchWrapper>();

        foreach ( var br in boundCalc.Branches.Objs) {
            var z1 = br.Obj.Node1.Zone;
            var z2 = br.Obj.Node2.Zone;
            var in1 = b.Zones.FirstOrDefault(m=>m.Id == z1.Id)!=null;
            var in2 = b.Zones.FirstOrDefault(m=>m.Id == z2.Id)!=null;
            if ( in1 ^ in2 ) {
                BoundCcts.Add(br,br.LineName);
            }
        }

        STripList = new Collection<Trip>();
        DTripList = new Collection<Trip>();

        autoTripList(boundCalc);

    }

    private double InterconnectionAllowance(double gin, double din, double dtot) {
        double x, t, t1, y;

        if ( din < 0) {
            x = gin - din;
        } else {
            x = gin + din;
        }

        x = 0.5 * x / dtot;

        t1 = (x - 0.5) / 0.5415;
        t = 1 - t1*t1;
        y = Math.Sqrt(t) * 0.0633 - 0.0243;
        return y * dtot;
    }

    public void InterconnectionTransfers( out double[] itfr, Nodes nodes) {
        int p, z;
        itfr = new double[nodes.Count];
        foreach( var nd in nodes.Objs) {
            p = nd.Pn;
            var zn = nd.Obj.Zone;
            if ( !nd.Obj.Ext ) {
                if ( Obj.Zones.FirstOrDefault(m=>m.Id == zn.Id)!=null) {
                    itfr[p] = kgin * nd.Obj.Generation - kdin * nd.Obj.Demand;
                } else {
                    itfr[p] = kgout * nd.Obj.Generation - kdout * nd.Obj.Demand;
                }
            }
        }
    }
    // Create the triplist for this boundary

    private void autoTripList(BoundCalc boundCalc) {
        int i=0, a, b;
        string nm, tstr;

        foreach( var br in BoundCcts.Items) { // Create single branch trip for each boundary circuit
            var tr  = new Trip();
            nm = $"S{i}";
            tr.OneBranch(nm,br);
            STripList.Add(tr,nm);
            i++;
        }

        for( a=1; a<BoundCcts.Count; a++) { // create all 2 boundary circuit combinations
            for ( b=a+1; b<=BoundCcts.Count;b++) {
                var tr = new Trip();
                nm = $"D{a},{b}";
                tr.Join(nm,STripList.Item(a),STripList.Item(b));
                DTripList.Add(tr,nm);
            }
        }

        if ( !string.IsNullOrEmpty(trips) ) { // create multi cct trips from any specified in trips
            var tsplit = trips.Split(';');
            for( i=0;i<tsplit.Length; i++ ) {
                nm = $"T{i}";
                tstr = tsplit[i];
                var tr = new Trip(nm,tstr,boundCalc.Branches);
                DTripList.Add(tr,nm);
            }
        }
    }

}