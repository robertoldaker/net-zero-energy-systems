using NHibernate.Linq;

namespace SmartEnergyLabDataApi.BoundCalc;

public class TopTrips {

    private BoundaryWrapper _bnd;
    private double _rtfer;
    private int _nsz;

    private List<BoundCalcAllTripsResult> _results;
    private BoundCalc _bc;

    public TopTrips(BoundCalc bc, int n) {
        _nsz = n;
        _bc = bc;
        _results = new List<BoundCalcAllTripsResult>();
    }

    public void Clear() {
        _results.Clear();
    }

    public void SetBoundary(BoundaryWrapper bnd, double rtfer) {
        _bnd = bnd;
        _rtfer = rtfer;
    }

    private void shuffle(int row) {
        if ( _results.Count<_nsz ) {
            return;
        } else {
            int i,j;
            for ( i=_nsz-2;i>=row;i--) {
                _results[i+1] = _results[i];
            }
        }
    }

    public List<BoundCalcAllTripsResult> Results {
        get{
            return _results;
        }
    }

    public void Insert(Trip tr, double[] bndcap, int[] mord, List<string> limitccts) {
        int i, j, m, spm, limits=0;
        double limitsu=0, su;
        bool first;
        string bn;

        first = true;        // signals first row of trip  must be output with setpoints
        j=0;
        if (limitccts.Count > 0 ) {
            spm = BoundCalc.SPAuto;
            limits = 1;      // signals limitccts must be output, 2 = check if cct in limitccts, 3 = ignore
        } else {
            spm = BoundCalc.SPMan;
        }

        for( i=0; i<_nsz;i++ ) {
            var tripResult = new BoundCalcAllTripsResult();
            m = mord[j];
            su = Math.Abs(bndcap[m]) - Math.Abs(_rtfer);
            if ( limits == 2) {
                if ( Math.Abs(su - limitsu) < 50 ) {
                    var br1 = _bc.Branches.get(m+1);
                    bn = br1.LineName;
                    if ( limitccts.Contains(bn) ) {
                        j++;
                        m = mord[j];
                        su = bndcap[m] - Math.Abs(_rtfer);
                    }
                } else {
                    limits = 3;
                }
            }
            //
            if ( i>=_results.Count || su < _results[i].Surplus ) {
                shuffle(i);

                if ( i>=_results.Count ) {
                    _results.Add(tripResult);
                } else {
                    _results[i] = tripResult;
                }

                tripResult.Capacity = bndcap[m] * Math.Sign(_rtfer);
                tripResult.Surplus = su;
                if ( tr == null ) {
                    tripResult.Trip = null;
                } else {
                    tripResult.Trip = new BoundCalcBoundaryTrips.BoundCalcBoundaryTrip(tr);
                }
                if ( first ) {
                    tripResult.Ctrls = new List<BoundCalcCtrlResult>();
                    foreach( var ctrl in _bc.Ctrls.Objs) {
                        tripResult.Ctrls.Add(new BoundCalcCtrlResult(ctrl, ctrl.GetSetPoint(spm)));
                    }
                    first = false;
                }
                var br = _bc.Branches.get(m+1);
                bn = br.LineName;
                if ( limits == 1) {
                    if ( limitccts.Contains(bn) ) {
                        limits = 2;
                        limitsu = su;
                        tripResult.LimCct = limitccts;
                        do {
                            j++;
                            if ( j>=mord.Length ) {
                                break;
                            }
                            br = _bc.Branches.get(mord[j] + 1);
                            bn = br.LineName;
                        } while(limitccts.Contains(bn));
                    } else {
                        tripResult.LimCct = new List<string>() { bn };
                        j++;
                    }
                } else {
                    tripResult.LimCct = new List<string>() { bn };
                    j++;
                }
                if ( j >= mord.Length) {
                    break;
                }
            }
        }

        return;

    }

}