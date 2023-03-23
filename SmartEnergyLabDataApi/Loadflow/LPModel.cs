/*
' Describes a part (or all) of a LP model
'
' Permits definition of (named) Variables and Constraints
'   Where Variables X must be positive and have positive costs
'   And Constraints are of the form A.X <= B
'
' Usage:
'   Call Init to start definitions
'   Call DefineVariable and DefineConstraint to describe the model
'
' L Dale 22 Feb 2017
' Revised to use collections and improve include functionality

Option Explicit
Option Base 0
Private LPVariables As New Collection    ' Variable definitions
Private LPConstraints As New Collection  ' Constraint definitions
  

Public Function VarCollection() As Collection
    Set VarCollection = LPVariables
End Function

Public Function ConsCollection() As Collection
    Set ConsCollection = LPConstraints
End Function

Public Function VarExists(name As String) As Boolean
    Dim def As LPVarDef
    
    On Error GoTo errorhandler
        Set def = LPVariables.Item(name)
        VarExists = True
    Exit Function
    
errorhandler:
    VarExists = False
End Function

Public Function ConsExists(name As String) As Boolean
    Dim def As LPConsDef
    
    On Error GoTo errorhandler
        Set def = LPConstraints.Item(name)
        ConsExists = True
    Exit Function
    
errorhandler:
    ConsExists = False
End Function

'
' Define variable, and its cost
'
Public Function VarDef(name As String, InitCons As String, Optional cost As Double = 0#) As LPVarDef
    Dim def As LPVarDef
    
    If VarExists(name) Then
        MsgBox "Repeated name: Variable " & name
    End If
    
    Set def = New LPVarDef
    With def
        .name = name
'        .Id = numv
        .cost = cost
        .InitC = InitCons
    End With
    
    LPVariables.Add def, name
    Set VarDef = def
End Function


'
' Define constraint of form magnitude > (variable * constant) ...
'
Public Function ConsDef(name As String, Equality As Boolean, Magnitude As Double, Pairs As Variant) As LPConsDef
    Dim i As Long
    Dim def As LPConsDef
    
    If ConsExists(name) Then
        MsgBox "Repeated name: Constraint " & name
    End If
       
    Set def = New LPConsDef
    With def
        .name = name
'        .Id = numc
        .Equality = Equality
        .Magnitude = Magnitude
        .Pairs = Pairs
    End With
    LPConstraints.Add def, name
    Set ConsDef = def
End Function

'
' Include a model in this model
' Update original variable and constraint definitions
'
Public Sub Include(model As LPModel)
    Dim vdef As LPVarDef
    Dim cdef As LPConsDef
    
    For Each vdef In model.VarCollection
        LPVariables.Add vdef, vdef.name
    Next vdef
    
    For Each cdef In model.ConsCollection
        LPConstraints.Add cdef, cdef.name
    Next cdef
End Sub

'
' Make LP from model
' NB Initial LP ordering of constraints matches model order
'
Public Function MakeLP() As LP
    Dim numv As Long
    Dim numc As Long
    Dim amat As SparseMatrix
    Dim bvec() As Double
    Dim cvec() As Double
    Dim vn() As String, vname As String
    Dim cn() As String, cname As String
    Dim mvs As Collection, mv As LPMSV
    Dim vdef As LPVarDef
    Dim cdef As LPConsDef
    Dim Pairs() As Variant
    Dim eqcvec() As Boolean, eqc As Boolean
    Dim i As Long, j As Long, cid As Long
    Dim rlp As LP

    numv = LPVariables.Count
    numc = LPConstraints.Count
    
    Set amat = New SparseMatrix
    amat.Init numc - 1, numv - 1
    ReDim bvec(numc - 1)
    ReDim eqcvec(numc - 1)
    ReDim cn(numc - 1)
    ReDim vn(numv - 1)
    ReDim cvec(numv - 1)
    Set mvs = New Collection

    For i = 0 To numv - 1
        Set vdef = LPVariables.Item(i + 1)
        With vdef
            .Id = i
            cvec(i) = .cost
            vn(i) = .name
            If Not .MOmgr Is Nothing Then   ' Create mv structure if a multisegment variable
                Set mv = New LPMSV
                mv.name = .name
                Set mv.mmo = New MO
                .MOmgr.Copy mv.mmo          ' copy in merit order
                mv.pos = .Ipos              ' initial position
                mv.vid = i                  ' and var id
                mvs.Add mv, .name
            End If
        End With
    Next i

    For i = 0 To numc - 1
        Set cdef = LPConstraints.Item(i + 1)
        With cdef
            .Id = i
            cn(i) = .name
            bvec(i) = .Magnitude
            eqcvec(i) = .Equality
            Pairs = .Pairs
        End With
        
'        If UBound(Pairs) < 1 Then
'            MsgBox "LP Build: No variables referenced by constraint " & cn(i)
'        End If
        For j = 0 To UBound(Pairs) Step 2
            vname = Pairs(j)
            If Not VarExists(vname) Then
                MsgBox "LP Build: Unknown variable " & vname & " in constraint " & cn(i)
            End If
            amat.Cell(i, VarId(vname)) = Pairs(j + 1)
        Next j
    Next i
    
    For Each mv In mvs
        Set vdef = LPVariables.Item(vn(mv.vid))
        If Not ConsExists(vdef.vzc) Then
            MsgBox "LP build: Unknown constraint " & vdef.vzc & " for multisegment variable " & vn(i)
        End If
        If Not ConsExists(vdef.vmc) Then
            MsgBox "LP build: Unknown constraint " & vdef.vmc & " for multisegment variable " & vn(i)
        End If
        If Not ConsExists(vdef.vdc) Then
            MsgBox "LP build: Unknown constraint " & vdef.vdc & " for multisegment variable " & vn(i)
        End If
        mv.dcid = ConsId(vdef.vdc)
        mv.mcid = ConsId(vdef.vmc)
        mv.zcid = ConsId(vdef.vzc)
    Next mv
    
    Set rlp = New LP
    rlp.Init amat, bvec, cvec, vn, cn
    
    'set the initial basis using initialisation info from model
    
    For i = 0 To numv - 1
        Set vdef = LPVariables.Item(vn(i))
        cname = vdef.InitC
        If Not ConsExists(cname) Then
            MsgBox "LP build: Unknown constraint " & cname & " initialising variable " & vn(i)
        End If
        cid = ConsId(cname)
        rlp.EnterBasis vdef.Id, cid
        If eqcvec(i) Then
            rlp.Equality(i) = True
        End If
    Next i

    Set MakeLP = rlp
End Function

' Provide the id of the named constraint

Public Function ConsId(name As String) As Long
    Dim cdef As LPConsDef
    
    Set cdef = LPConstraints.Item(name)
    ConsId = cdef.Id
End Function

' Provide the id of named variable

Public Function VarId(name As String) As Long
    Dim vdef As LPVarDef
    
    Set vdef = LPVariables.Item(name)
    VarId = vdef.Id
End Function

Public Function Test() As Boolean
    Dim dcdef As LPConsDef
    Dim gvdef As LPVarDef
    Dim gzc As LPConsDef
    Dim gmc As LPConsDef
    Dim mmo As New MO
    Dim mlp As LP, rc1 As Long, rc2 As Long
    Dim expected As Double, res As Double
    
    mmo.Add "G1", 10#, 200#
    mmo.Add "G2", 20#, 300#
    mmo.Add "G3", 30#, 400#
    
    Set gvdef = Me.VarDef("gvar", "gzc")
    Set dcdef = Me.ConsDef("dem", False, -550#, Array("gvar", 1#))
    Set gzc = Me.ConsDef("gzc", False, 0#, Array("gvar", 1#))
    Set gmc = Me.ConsDef("gmc", False, 999#, Array("gvar", -1#))
    
    With gvdef
        Set .MOmgr = mmo
        .vzc = "gzc"
        .vmc = "gmc"
        .vdc = "dem"
    End With
    
    Set mlp = Me.MakeLP
    rc1 = mlp.SolveLP(rc2)
    
'    Debug.Print "G1"; mlp.SegmentDispatch("gvar", "G1")
'    Debug.Print "G2"; mlp.SegmentDispatch("gvar", "G2")
'    Debug.Print "G3"; mlp.SegmentDispatch("gvar", "G3")
    
    expected = 10# * 200# + 20# * 300# + (550# - 200# - 300#) * 30#
    res = -mlp.Objective
    Test = (expected = res)
End Function

*/
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

    public class LPModel {

        private Collection<LPVarDef> LPVariables;
        private Collection<LPConsDef> LPContraints;
        public LPModel() {
            LPVariables = new Collection<LPVarDef>();
            LPContraints = new Collection<LPConsDef>();
        }

        public Collection<LPVarDef> VarCollection {
            get {
                return LPVariables;
            }
        }

        public Collection<LPConsDef> ConsCollection {
            get {
                return LPContraints;
            }
        }

        public bool VarExists(string name) {
            return LPVariables.ContainsKey(name);
        }

        public bool ConsExists(string name) {
            return LPContraints.ContainsKey(name);
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
            LPContraints.Add(def,name);
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
                LPContraints.Add(cdef,cdef.name);
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
            Collection<LPMSV> mvs;
            LPVarDef vdef;
            LPConsDef cdef;
            object[] Pairs=null;
            bool[] eqcvec;
            bool eqc;
            int i, j, cid;
            LP rlp;

            numv = LPVariables.Count;
            numc = LPContraints.Count;

            amat = new SparseMatrix(numc-1,numv-1);
            bvec = new double[numc];
            eqcvec = new bool[numc];
            cn = new string[numc];
            vn = new string[numv];
            cvec = new double[numv];
            mvs = new Collection<LPMSV>();

            for( i=0; i<numv;i++) {
                vdef = LPVariables.Item(i+1);
                vdef.Id = i;
                cvec[i] = vdef.cost;
                vn[i] = vdef.name;
                if ( !(vdef.MOmgr==null) ) { // Create mv structure if a multisegment variable
                    var mv = new LPMSV();
                    mv.name = vdef.name;
                    mv.mmo = new MO();
                    vdef.MOmgr.Copy(mv.mmo); // copy in merit order
                    mv.pos = vdef.Ipos;      // initial position
                    mv.vid = i;              // and var id
                    mvs.Add(mv, vdef.name);
                }
            }

            for(i=0;i<numc;i++) {
                cdef = LPContraints.Item(i+1);
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

            foreach(LPMSV mv in mvs.Items) {
                vdef = LPVariables.Item(vn[mv.vid]);
                if ( !ConsExists(vdef.vzc)) {
                    throw new Exception($"LP Build: Unknown contraint {vdef.vzc} for multisegment variable {vn[i]}");
                }
                if( !ConsExists(vdef.vmc)) {
                    throw new Exception($"LP Build: Unknown constraint {vdef.vmc} for multisegment variable {vn[i]}");
                }
                if (!ConsExists(vdef.vdc)) {
                    throw new Exception($"LP Build: Unknown constraint {vdef.vdc} for multisegment variable {vn[i]}");
                }
                mv.dcid = ConsId(vdef.vdc);
                mv.mcid = ConsId(vdef.vmc);
                mv.zcid = ConsId(vdef.vzc);
            }

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
            cdef = LPContraints.Item(name);
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
            LPVarDef gvdef;
            LPConsDef gzc;
            LPConsDef gmc;
            MO mmo = new MO();
            LP mlp;
            int rc1, rc2=0;
            double expected, res;

            mmo.Add("G1", 10, 200);
            mmo.Add("G2", 20, 300);
            mmo.Add("G3", 30, 400);

            gvdef = this.VarDef("gvar", "gzc");
            dcdef = this.ConsDef("dem", false, -550, new object[] { "gvar", 1});
            gzc = this.ConsDef("gzc", false, 0, new object[] {"gvar", 1});
            gmc = this.ConsDef("gmc", false, 999, new object[] {"gvar", -1});

            gvdef.MOmgr = mmo;
            gvdef.vzc = "gzc";
            gvdef.vmc = "gmc";
            gvdef.vdc = "dem";

            mlp = this.MakeLP();
            rc1 = mlp.SolveLP(ref rc2);

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
        public MO MOmgr {get; set; } // multisegment data
        public int Ipos {get; set;} // initial segment
        public string vzc {get; set;} // name of the zero contraint on variable
        public string vmc {get; set;} // name of maximum contraint on variable
        public string vdc {get; set;} // name of auxiliary/demand contraint
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