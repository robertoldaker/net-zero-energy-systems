using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Routing;
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data.Loadflow;

namespace SmartEnergyLabDataApi.BoundCalc;

public class Trip {
    public string name {get; set;}
    public int nbr {get; set;}
    public int nac {get; set;}
    private BranchWrapper[] _ccts;
    public static BoundCalc BoundCalc {get; set;}

    public Trip() {

    }

    // Identify branches in tripstring (comma delimited longnames)
    // Order branches so ac circuits first
    public Trip(string tripname, string tripstring, Branches branches) {        
        string[] strip;
        int i, b, t;
        string bname;
        BranchWrapper br;

        name = tripname;
        strip = tripstring.Split(',');
        nbr = strip.Length-1;
        _ccts = new BranchWrapper[strip.Length];
        b = 0;
        t = nbr;

        for ( i=0; i<=nbr;i++) {
            bname = strip[i];
            if ( !branches.Exists(bname) ) {
                throw new Exception($"Branch [{bname}] not found in trip [{tripstring}]");
            } else {
                br = branches.get(bname);
                if ( br.Obj.X != 0  ) {
                    _ccts[b] = br;
                    b++;
                } else {
                    _ccts[t] = br;
                    t--;
                }
            }
        }
        nac = b-1;
    }

    public BranchWrapper[] Branches{
        get {
            return _ccts;
        }
    }

    public BranchWrapper GetCircuit(int n) {
        return _ccts[n];        
    }

    // Creator for single circuit trip

    public bool OneBranch(string tripname, BranchWrapper br) {
        name = tripname;
        nbr = 0;
        _ccts = new BranchWrapper[1];
        _ccts[0] = br;
        if ( br.Obj.X !=0 ) {
            nac = 0;
        } else {
            nac = -1;
        }
        return true;
    }

    // Creator which joins 2 trips

    public bool Join(string tripname, Trip trip1, Trip trip2) {
        int i, b, t;
        BranchWrapper br;

        name = tripname;
        nbr = trip1.nbr + trip2.nbr + 1;
        _ccts = new BranchWrapper[nbr+1];
        b=0;
        t=nbr;

        for ( i=0;i<=trip1.nbr;i++) {
            br = trip1.GetCircuit(i);
            if ( br.Obj.X!=0) {
                _ccts[b] = br;
                b++;
            } else {
                _ccts[t] = br;
                t--;
            }
        }
        nac = b - 1;
        return true;
    }

    // Provide trip description (list of branch longnames)

    public string TripDescription() {
        int i;
        var td = _ccts[0].LineName;
        for ( i=1;i<=nbr;i++) {
            td+=$",{_ccts[i].LineName}";
        }
        return td;
    }

    // Deactivate

    public void Deactivate() {
        int i;
        for ( i=0;i<=nbr;i++) {
            _ccts[i].BOut = false;
        }
    }

    // Activate trip by computing trip sensmat and flagging branches as outaged
    // returns fals if trip no ac cisrcuits in trip or splits ac network

    public bool Activate(out double[,] sensmat) {
        int i;
        var activate = CalcSensMat(out sensmat);
        for (i=0;i<=nbr;i++) {
            _ccts[i].BOut = true;
        }
        return activate;
    }

    // Calc branch end injection sensitivity to branch flow = (I - Fsens)^-1
    // returns fals if (I-Fsens) is singular

    private bool CalcSensMat(out double[,] sensmat) {
        double[] tvec;
        double [,] mat;
        double d;
        int i,j;

        if ( nac < 0 ) {
            sensmat = new double[0,0];
            return false;
        }

        mat = new double[nac,nac];
        sensmat = new double[nac,nac];

        for(j=0;j<=nac;j++) {
            tvec = new double[BoundCalc.Nodes.Count - 1];
            tvec[_ccts[j].pn1] = 1;
            tvec[_ccts[j].pn2] = -1;
            BoundCalc._ufac.Solve(tvec,ref tvec); // calculate vang caused by unit injections at branch ends

            for( i=0;i<=nac;i++) {
                if ( i==j ) {
                    mat[i,j] = 1;
                }
                mat[i,j] = mat[i,j] - (tvec[_ccts[i].pn1] - tvec[_ccts[i].pn2]) * BoundCalc.PUCONV / _ccts[i].Obj.X;
            }            
        }

        //
        d = Utilities.Determinant(mat);
        if ( Math.Abs(d)<=LPhdr.lpEpsilon) {            
            return false;
        }
        //
        var res = Utilities.MatrixInverse(mat);

        if ( nac==0 ) {
            sensmat[0,0] = res[0,0];
        } else {
            for( i=0;i<=nac;i++) {
                for(j=0;j<=nac;j++) {
                    sensmat[i,j] = res[i,j];
                }
            }
        }
        return true;
    }

    public void TripSolve(double[,] sensmat, double[] ovang, out double[] tvang) {
        int i, j;
        double[] f, inj, tvec;

        f=new double[nac+1];
        inj = new double[nac+1];
        tvec = new double[ovang.Length];

        // Calc original flows on tripped branches

        BranchWrapper bw;
        for(i=0;i<=nac;i++) {
            bw = _ccts[i];
            f[i] = (ovang[bw.pn1] - ovang[bw.pn2]) * BoundCalc.PUCONV / bw.Obj.X;
        }

        // Calc required injections

        for( i=0;i<=nac;i++) {
            bw = _ccts[i];
            for(j=0;j<=nac;j++) {
                inj[i] = inj[i] + sensmat[i,j] * f[j];
            }
            tvec[bw.pn1] = tvec[bw.pn1] + inj[i];
            tvec[bw.pn1] = tvec[bw.pn2] - inj[i];
        }

        //
        BoundCalc._ufac.Solve(tvec, ref tvec);
        tvang = Utilities.CopyArray(ovang);

        for (i=0;i<ovang.Length;i++) {
            tvang[i] = tvang[i] + tvec[i];
        }
    }

    public bool TripVectors(double[]?[] civang, out double[]?[] tcvang) {
        int i, nc;
        double[,] sensmat;
        double[] tv, ntv;
        CtrlWrapper ct;
        BranchWrapper br;

        nc = civang.Length-1; // upb of controls + ia sensitivity

        tcvang = new double[nc][];

        if ( !Activate(out sensmat)) { // might be dc ccts
            if ( nac>=0 ) {
                return false;          // some ac ccts present to ac trip splits network
            }

            // no ac circuits in trip so must be hvdc trip
            tcvang[0] = civang[0];      // base vang unchanged
            tcvang[nc] = civang[nc];    // ia sensitivity (if present) unchanged

            for (i=1; i<=nc-1;i++) {
                ct = BoundCalc.Ctrls.get(i);
                br = ct.Branch;
                if ( !br.BOut  ) {
                    tcvang[i] = civang[i];   // control vang unchanged if hvdc not tripped (empty if control outaged)
                }
            }
        } else {
            // udate base vang
            tv = civang[0];
            TripSolve( sensmat, tv, out ntv);
            tcvang[0] = ntv;
            // update ia sensitivity (if present)
            if ( civang[nc]!=null ) {
                tv = civang[nc];
                TripSolve(sensmat, tv, out ntv);
                tcvang[nc] = ntv;
            }
            for ( i=1;i<=nc-1;i++) {
                ct = BoundCalc.Ctrls.get(i);
                br = ct.Branch;
                if ( !br.BOut ) {
                    if ( civang[i]!=null ) {
                        tv = civang[i];
                        TripSolve(sensmat, tv, out ntv);
                        tcvang[i] = ntv;
                    } else {
                        tcvang[i] = civang[i];
                    }
                } else {
                    // leave tcvang[i] empty
                }
            }
        }        
        return true;
    }


}