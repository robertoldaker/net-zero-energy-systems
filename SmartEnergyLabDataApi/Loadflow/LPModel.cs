using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Loadflow
{
    //
    //Describes a part (or all) of a LP model
    // Permits definition of (named) Variables and Constraints
    //   Where Variables X must be positive and have positive costs
    //   And Constraints are of the form A.X <= B
    //
    // Usage:
    //   Call Init to start definitions
    //   Call DefineVariable and DefineConstraint to describe the model
    //
    // L Dale 22 Feb 2017
    // Revised to use collections and improve include functionality
    // 17 Nov 2023 Removed multisegment variables

    public class LPModel {

        private Collection<LPVarDef> LPVariables;
        private Collection<LPConsDef> LPConstraints;
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

        public LPVarDef VarDef( string name, string InitCons, double cost = 0) {
            if ( VarExists(name) ) {
                throw new Exception($"Repeated name: Variable {name}");
            }
            var def = new LPVarDef();
            def.name = name;
            //def.Id = numv;
            def.cost = cost;
            def.InitC = InitCons;
            LPVariables.Add(def, name);
            return def;
        }

        //
        // Define constraint of form magnitude > (variable * constant) ...
        //
        public LPConsDef ConsDef(string name, bool Equality, double Magnitude, object[] Pairs) {
            LPConsDef def;
            if ( ConsExists(name) ) {
                throw new Exception($"Repeated name: Contraint {name}");
            }
            def = new LPConsDef();
            def.name = name;
            //de.Id = numc;
            def.Equality = Equality;
            def.Magnitude = Magnitude;
            def.Pairs = Pairs;
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
            int numv, numc;
            SparseMatrix amat;
            double[] bvec, cvec;
            string[] vn, cn;
            string vname, cname;
            //Collection<LPMSV> mvs;
            LPVarDef vdef;
            LPConsDef cdef;
            object[] Pairs=null;
            bool[] eqcvec;
            bool eqc;
            int i, j, cid;
            LP rlp;

            numv = LPVariables.Count;
            numc = LPConstraints.Count;

            amat = new SparseMatrix(numc-1,numv-1);
            bvec = new double[numc];
            eqcvec = new bool[numc];
            cn = new string[numc];
            vn = new string[numv];
            cvec = new double[numv];
           // mvs = new Collection<LPMSV>();

            for( i=0; i<numv;i++) {
                vdef = LPVariables.Item(i+1);
                vdef.Id = i;
                cvec[i] = vdef.cost;
                vn[i] = vdef.name;
                //if ( !(vdef.MOmgr==null) ) { // Create mv structure if a multisegment variable
                //    var mv = new LPMSV();
                //    mv.name = vdef.name;
                //    mv.mmo = new MO();
                //    vdef.MOmgr.Copy(mv.mmo); // copy in merit order
                //    mv.pos = vdef.Ipos;      // initial position
                //    mv.vid = i;              // and var id
                //    mvs.Add(mv, vdef.name);
                //}
            }

            for(i=0;i<numc;i++) {
                cdef = LPConstraints.Item(i+1);
                cdef.Id = i;
                cn[i] = cdef.name;
                bvec[i] = cdef.Magnitude;
                eqcvec[i] = cdef.Equality;
                Pairs = cdef.Pairs;
                //if ( Pairs==null || Pairs.Length==0 ) {
                //    throw new Exception($"LP Build: No variables referenced by contraint {cn[i]}");
                //}
                for( j=0; j<Pairs.Length;j+=2) {
                    vname = (string) Pairs[j];
                    if ( !VarExists(vname) ) {
                        throw new Exception($"LP Build: Unknown variable {vname} in constraint {cn[i]}");
                    }
                    double v = Convert.ToDouble(Pairs[j+1]);
                    amat.SetCell(i, VarId(vname),v);
                }
            }

            //foreach(LPMSV mv in mvs.Items) {
            //    vdef = LPVariables.Item(vn[mv.vid]);
            //    if ( !ConsExists(vdef.vzc)) {
            //        throw new Exception($"LP Build: Unknown contraint {vdef.vzc} for multisegment variable {vn[i]}");
            //    }
            //    if( !ConsExists(vdef.vmc)) {
            //        throw new Exception($"LP Build: Unknown constraint {vdef.vmc} for multisegment variable {vn[i]}");
            //    }
            //    if (!ConsExists(vdef.vdc)) {
            //        throw new Exception($"LP Build: Unknown constraint {vdef.vdc} for multisegment variable {vn[i]}");
            //    }
            //    mv.dcid = ConsId(vdef.vdc);
            //    mv.mcid = ConsId(vdef.vmc);
            //    mv.zcid = ConsId(vdef.vzc);
            //}

            rlp= new LP(amat,bvec,cvec,vn,cn);

            // set the initial basis using initialisation info from model
            for(i=0;i<numv;i++) {
                vdef = LPVariables.Item(vn[i]);
                cname = vdef.InitC;
                if (!ConsExists(cname)) {
                    throw new Exception($"LP build: unknown contraint {cname} initialising variable {vn[i]}");
                }
                cid = ConsId(cname);
                rlp.EnterBasis(vdef.Id,cid);
                if ( eqcvec[i]) {
                    rlp.SetEquality(i, true);
                }
            }
            return rlp;
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
            LPVarDef[] gvdef = new LPVarDef[3];
            LPConsDef[] gzc = new LPConsDef[3];
            LPConsDef[] gmc = new LPConsDef[3];
            //MO mmo = new MO();
            LP mlp;
            int rc1, rc2=0;
            double expected, res;

            //mmo.Add("G1", 10, 200);
            //mmo.Add("G2", 20, 300);
            // mmo.Add("G3", 30, 400);


            // Create 3 gen variables with min and max constraints
            gvdef[0] = this.VarDef("g0var","g0zc", 10);
            gvdef[1] = this.VarDef("g1var","g1zc", 20);
            gvdef[2] = this.VarDef("g2var","g2zc", 30);

            gzc[0] = this.ConsDef("g0zc", false, 0, new object[] { "g0var, 1"});
            gzc[1] = this.ConsDef("g1zc", false, 0, new object[] { "g1var, 1"});
            gzc[2] = this.ConsDef("g2zc", false, 0, new object[] { "g2var, 1"});
            gmc[0] = this.ConsDef("g0mc", false, 200, new object[] { "g0var", -1});
            gmc[1] = this.ConsDef("g1mc", false, 300, new object[] { "g1var", -1});
            gmc[2] = this.ConsDef("g2mc", false, 400, new object[] { "g2var", -1});

            dcdef = this.ConsDef("demc", false, -550, new object[] { "g0var", 1, "g1var", 1, "g2var", 1});
            //gzc = this.ConsDef("gzc", false, 0, new object[] {"gvar", 1});
            //gmc = this.ConsDef("gmc", false, 999, new object[] {"gvar", -1});

            //gvdef.MOmgr = mmo;
            //gvdef.vzc = "gzc";
            //gvdef.vmc = "gmc";
            //gvdef.vdc = "dem";

            mlp = this.MakeLP();
            rc1 = mlp.SolveLP(ref rc2);

            Console.WriteLine($"G1 {mlp.Slack(ConsId("g0zc"))}");
            Console.WriteLine($"G2 {mlp.Slack(ConsId("g1zc"))}");
            Console.WriteLine($"G3 {mlp.Slack(ConsId("g2zc"))}");

            expected = 10 * 200 + 20 * 300 + (550 - 200 - 300) * 30;
            res = -mlp.Objective();

            return expected == res;
        }
    }

    public class LPVarDef {
        public string name {get; set;}
        public int Id {get; set;}
        public double cost {get; set;}
        public string InitC {get; set; } // name of constraint which provides initial value
        //public MO MOmgr {get; set; } // multisegment data
        //public int Ipos {get; set;} // initial segment
        //public string vzc {get; set;} // name of the zero contraint on variable
        //public string vmc {get; set;} // name of maximum contraint on variable
        //public string vdc {get; set;} // name of auxiliary/demand contraint
    }

    public class LPConsDef {
        public string name {get; set;}
        public int Id {get; set;}
        public bool Equality {get; set;}
        public double Magnitude {get; set;}
        public object[] Pairs {get; set;}
        public void augment(string varname, double coefficient) {
            Pairs = Pairs.Concat(new object[]{ varname, coefficient }).ToArray();
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