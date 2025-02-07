using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.BoundCalc
{
    // Describes a part (or all) of a LP model
    //
    // Permits definition of (named) Variables and Constraints
    // for an LP which maximises Ct.X Such that A.X <= B and X>0
    // To minimise Ct.X make C -ve
    //
    // Usage:
    //   Call Init to start definitions
    //   Call DefineVariable and DefineConstraint to describe the model
    //
    // L Dale 22 Feb 2017
    // Revised to use collections and improve include functionality
    // 17 Nov 2023 Removed multisegment variables
    // 1 Dec 2023 Incorporated references to X>=0 and X <= Max constraints in variable definitions. Added paired variable to represent negative values.
    // Types of constraints extended to <=, >= and =

    public class LPModel {

        private Collection<LPVarDef> LPVariables;
        private Collection<LPConsDef> LPConstraints;
        private int errors;
        public LPModel() {
            LPVariables = new Collection<LPVarDef>();
            LPConstraints = new Collection<LPConsDef>();
        }

        public Collection<LPVarDef> VarCollection {
            get {
                return LPVariables;
            }
        }

        public Collection<LPConsDef> ConsCollection {
            get {
                return LPConstraints;
            }
        }

        public bool Exists<T>(string name, Collection<T> col) {
            return col.ContainsKey(name);
        }

        public bool VarExists(string name) {
            return Exists(name, LPVariables);
        }

        public bool ConsExists(string name) {
            return Exists(name, LPConstraints);
        }

        //
        // Define lp variable, and optionally: name of initially determining constraint, non-zero welfare and maximum value
        // NB all vsrisbles will have a zero constraint (varname plus suffix 'zc') and this will determine the initial value of the variable
        // If maxvalue (>=0) is given a new maxvalue constraint is created (varname plus suffix 'mc')
        // To minimise Ct.X make welfare -ve
        public LPVarDef VarDef( string varname, string initCons = "Unset", double welfare = 0, double maxValue=-1) {
            if ( VarExists(varname) ) {
                errors+=1;
                throw new Exception($"Repeated name: Variable {varname}");                
            }
            var varDef = new LPVarDef();
            LPVariables.Add(varDef, varname);

            varDef.name = varname;
            varDef.Welfare = welfare;
            varDef.Vzc = ConsDef(varname + "zc", LPhdr.CTGTE, 0, new object[] {varname,1} );

            if ( initCons == "Unset" ) { // set variable to zero by default
                varDef.InitC = varname + "zc";
            }

            if ( !(maxValue < 0) ) {
                varDef.Vmc = ConsDef(varname + "mc", LPhdr.CTLTE, maxValue, new object[] {varname,1} );
            }
            return varDef;
        }

        // Define lp variable positive & negative pair.  Initially determining constraints for positive and negative variables are zero constraints.
        // , non-zero cost, maximum value, presence of negative range variable and its min value
        // NB all vsrisbles will have a zero constraint (varname plus suffix 'zc') and this will determine the initial value of the variable unless another constraint is named
        // If maxvalue (>=0) is given a new maxvalue constraint is created (varname plus suffix 'mc')
        // The 'neg// flag signals the variable should have a companion for negative range (varname plus suffix 'n') with corresponding zero constraint
        // If minvalue (<0) is given a new minvalue constraint is created for the (varname plus suffix 'nmc')
        // To minimise Ct.X make costs -ve
        public LPVarDef PairDef(string varname, double pWelfare = 0, double nWelfare=0, double maxValue = -1, double minValue = 1) {
            if ( VarExists(varname)) {
                errors+=1;
                throw new Exception($"Repeated name: Variable {varname}");                
            }
            var pairDef = new LPVarDef();
            LPVariables.Add(pairDef, varname);
            pairDef.name = varname;
            pairDef.Vpv = VarDef(varname + "p", welfare:pWelfare, maxValue: maxValue);
            pairDef.Vnv = VarDef(varname + "n", welfare:nWelfare, maxValue: -minValue);
            return pairDef;
        }


        //
        // Define constraint of form pairs (variable * constant) <=/>=/= magnitude
        //
        public LPConsDef ConsDef(string name, int cType, double Magnitude, object[] Pairs) {
            LPConsDef def;

            if ( ConsExists(name) ) {
                errors+=1;
                throw new Exception($"Repeated name: Contraint {name}");
            }

            def = new LPConsDef();
            def.name = name;
            //def.Id = numc;
            def.CType = cType;
            def.Magnitude = Magnitude;
            def.Pairs=Utilities.CopyArray(Pairs);
            LPConstraints.Add(def,name);
            return def;
        }

        //
        // Include a model in this model
        // Update original variable and constraint definitions
        //
        public void Include(LPModel model) {
            foreach(LPVarDef vdef in model.VarCollection.Items) {
                LPVariables.Add(vdef,vdef.name);
            }
            foreach(LPConsDef cdef in model.ConsCollection.Items) {
                LPConstraints.Add(cdef,cdef.name);
            }
        }

        //
        // Make LP from model
        // NB Initial LP Ordering of contraints matches model order
        //
        public LP MakeLP() {
            int numv=0, numc;
            SparseMatrix amat;
            double[] bvec, cvec;
            string[] vn, cn;
            string vname, cname;
            //LPVarDef vdef;
            //LPConsDef cdef;
            object[] Pairs=null;
            bool[] eqcvec;
            double sn=0;
            int i=0, j, cid;
            LP rlp;

            numc = LPConstraints.Count;
            foreach (var vdef in LPVariables.Items) {
                if ( vdef.Vpv == null) { // not a pair
                    numv+=1;
                }
            }

            amat = new SparseMatrix(numc-1,numv-1);
            bvec = new double[numc];
            eqcvec = new bool[numc];
            cn = new string[numc];
            vn = new string[numv];
            cvec = new double[numv];

            // Process variable definitions
            i = 0;
            foreach( var vdef in LPVariables.Items ) {
                if ( vdef.Vpv == null ) {
                    vdef.Id = i;
                    cvec[i] = vdef.Welfare;
                    vn[i] = vdef.name;
                    i = i + 1;
                }
            }

            // Process constraint definitions
            i=0;
            foreach( var cdef in LPConstraints.Items) {
                cdef.Id = i;
                cn[i] = cdef.name;
                if ( cdef.CType == LPhdr.CTLTE ) {
                    sn = 1;
                } else {
                    sn = -1; // Change sign of pair coefs and magnitude on - and >= constraints
                }
                bvec[i] = cdef.Magnitude * sn;
                eqcvec[i] = ( cdef.CType == LPhdr.CTEQ );
                Pairs = Utilities.CopyArray(cdef.Pairs);

                for ( j=0; j<Pairs.Length; j+=2) {
                    vname = (string) Pairs[j];
                    if ( !VarExists(vname) ) {
                        errors+=1;
                        throw new Exception($"LP Build: Unknown variable {vname} in contraint {cn[i]}");
                    }
                    var vdef = LPVariables.Item(vname);
                    double p = objectToDouble(Pairs[j+1]);
                    if ( vdef.Vpv == null  ) {
                        amat.SetCell(i, vdef.Id, p * sn);
                    } else {
                        amat.SetCell(i, vdef.Vpv.Id, p * sn);
                        amat.SetCell(i, vdef.Vnv.Id, -p * sn);
                    }
                }
                i = i + 1;
            }
        
            rlp= new LP(amat,bvec,cvec,vn,cn);

            foreach( var vdef in LPVariables.Items) {
                if ( vdef.Vpv == null ) {
                    cname = vdef.InitC;
                    if ( !ConsExists(cname) ) {
                        errors+=1;
                        throw new Exception($"LP build: unknown constraint {cname} initialising variable {vn[i]}");
                    }
                    cid =ConsId(cname);
                    rlp.EnterBasis(vdef.Id, cid);
                    if ( eqcvec[cid] ) {
                        rlp.SetEquality(cid, true);
                    }
                }
            }

            if ( errors>0 ) {
                throw new Exception("Too many errors to complete LPModel build");
            }

            return rlp;
        }

        private double objectToDouble(object obj) {
            if ( obj is int ) {
                return (double) (int) obj;
            } else if ( obj is double) {
                return (double) obj;
            } else {
                throw new Exception($"Unexpected type of object found [{obj.GetType().Name}]");
            }
        }

        // Provide the id of the named constraint
        public int ConsId(string name) {
            LPConsDef cdef;
            cdef = LPConstraints.Item(name);
            return cdef.Id;
        }

        // Provide the id of named variable
        public int VarId(string name) {
            LPVarDef vdef;
            vdef= LPVariables.Item(name);
            return vdef.Id;
        }
        
        public bool Test() {
            LPConsDef dcdef;
            LPVarDef[] gvdef = new LPVarDef[3]; // generation variables

            LP mlp;
            int rc1, rc2=0;
            double expected, res;

            // Create 3 gen variables with min and max contraints (-ve cost to minimise)
            gvdef[0] = VarDef("g0",welfare:-10,maxValue:200);
            gvdef[1] = VarDef("g1",welfare:-20,maxValue:300);
            gvdef[2] = VarDef("g2",welfare:-30,maxValue:400);

            dcdef = ConsDef("demc",LPhdr.CTEQ,550, new object[] { "g0",1,"g1",1,"g2",1 });

            mlp = this.MakeLP();
            rc1 = mlp.SolveLP(ref rc2);

            Console.WriteLine($"G1 {gvdef[0].Value(mlp)}");
            Console.WriteLine($"G2 {gvdef[1].Value(mlp)}");
            Console.WriteLine($"G3 {gvdef[2].Value(mlp)}");
            Console.WriteLine($"Price {dcdef.Shadow(mlp)}");

            expected = -10 * 200 - 20*300 - (550 - 200 - 300) * 30;
            res = mlp.Objective();            
            return expected == res;
        }
    }

    public class LPVarDef {
        public string name {get; set;}
        public int Id {get; set;}
        public double Welfare {get; set;} // actually welfare since +ve values maximised
        public string InitC {get; set; } // name of constraint which provides initial value
        public LPConsDef Vzc {get; set;} // the zero constraint on variable
        public LPConsDef Vmc {get; set;} // the maximum constrint on variable
        public LPVarDef Vpv {get; set;}  // the positive of a variable pair
        public LPVarDef Vnv {get; set;}  // the negative of a variable pair

        public double Value(LP mlp) {
            if ( Vpv == null ) {
                return mlp.Slack(Vzc.Id);
            } else {
                return Vpv.Value(mlp) - Vnv.Value(mlp);
            }
        }
    }

    public class LPConsDef {
        public string name {get; set;}
        public int Id {get; set;}
        public int CType {get; set;} // CTLTE, CTGTE or CTEQ
        public double Magnitude {get; set;}
        public object[] Pairs {get; set;}
        public void augment(string varname, double coefficient) {
            Pairs = Pairs.Concat(new object[]{ varname, coefficient }).ToArray();
        }

        public double Slack(LP mlp) {
            return mlp.Slack(Id);
        }

        public double Shadow(LP mlp) {
            return mlp.Shadow(Id);
        }

    }

    public class LPMSV {
        
        public string name; // used as key to find msv
        public MO mmo;      // the merit order handler
        public int pos;     // current segment
        public int vid;     // variable index in LP
        public int zcid;    // zero constraint in LP
        public int mcid;    // max constraint in LP
        public int dcid;    // the demand or auxiliary constraint        
        
    }


}