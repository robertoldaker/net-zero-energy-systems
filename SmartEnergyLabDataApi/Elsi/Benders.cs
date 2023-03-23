/*
' Implement and manage the Bender's decomposition model part
'
' Lewis Dale 5 July 2018
' Converted to implement IPart 26 Jan 2019

Option Explicit
Option Base 0

Implements IPart
Implements IMaster

Const maxcut As Long = 50   ' Maximum number of benders cuts
Const maxrecovery = 2       ' Maximum retries of bad master
Const btol As Double = 0.0005 ' .05% of objective

Private mlp As LP           ' master problem lp
Private mvlist() As Variant ' master variable list
Private cmvars() As Double  ' current master variables
Private lmvars() As Double  ' previous master variables
Private spsame() As Boolean ' has subproblem(i)changed?
Private splp() As LP        ' subproblem lps
Private sprq() As Long      ' subproblem return codes
Private sprc() As Long      ' subproblem return2
Private spobj() As Double   ' supproblem objective result
Private spiter() As Long    ' number of times subproblem solved
Private spcosts() As Double ' extra sub problem costs
Private spscale() As Double ' sub problem scaling factor (i.e.number of hours)
Private margp() As Variant  ' marg prices for subproblems
Private vmax As Long        ' upb master vars
Private pmax As Long        ' upb supproblems
Private bcc() As LPConsDef  ' bender cuts
Private wtvar As LPVarDef   ' the weight variable
Private zp() As Variant     ' save zone prices
Private savesmp() As Variant
Private wt() As Double      ' cut weights

Private pname As String


Public Property Get IPart_partname() As String
    IPart_partname = pname
End Property

Public Property Let IPart_partname(ByVal rhs As String)
    pname = rhs
End Property

' Build the model part
' Create weight variable and blank cuts

Public Sub Ipart_Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)
    Dim i As Long, j As Long
    
    ReDim bcc(maxcut) As LPConsDef
       
    'initialise first cut as optimality cut with blank data
    Set bcc(0) = lpm.ConsDef("cut" + CStr(0), False, 0#, Array("wtv", 1#))
    
    Set wtvar = lpm.VarDef("wtv", bcc(0).name, 1#)
    
    ' remaining cuts empty
    For i = 1 To maxcut
        Set bcc(i) = lpm.ConsDef("cut" + CStr(i), False, 0#, Array())
    Next i
End Sub

Public Sub Ipart_Update(mlp As LP, Optional csel As Variant)
    'do nothing
End Sub

Public Sub Ipart_SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)
    ' do nothing
End Sub

' skip all cuts except first which is used to set wtvar

Public Sub Ipart_Initialise(mlp As LP, smp() As Variant)
    Dim i As Long
    Dim k As Long, v As Long, d As Long
            
    ReDim zp(maxcut) As Variant
    savesmp = smp
    
    ' skip all cuts (nb active cuts remain in basis)
    For i = 0 To maxcut
        mlp.Skip(bcc(i).Id) = True
    Next i
    
    v = wtvar.Id
    k = bcc(0).Id
    mlp.EnterBasis v, k
End Sub


' Output the zone prices

Public Sub Ipart_Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)
    Dim i As Long, j As Long, k As Long, c As Long
    Dim zmax As Long, pmax As Long
    Dim p() As Variant, q() As Double, chk As Double
    
    pmax = pertab.RowCount - 1
    zmax = ztab.RowCount - 1
    
    ReDim oparray(1 To zmax + 1, 1 To pmax + 1) As Variant
    ReDim wt(maxcut) As Double
    
    For k = 0 To maxcut
        c = bcc(k).Id
        If Not mlp.Skip(c) And mlp.InBasis(c) Then
            wt(k) = mlp.Shadow(c)
            chk = chk + wt(k)
            p = zp(k)
            For j = 0 To pmax
                q = p(j)
                For i = 0 To zmax
                    oparray(i + 1, j + 1) = oparray(i + 1, j + 1) + wt(k) * q(i)
                Next i
            Next j
        End If
    Next k
End Sub

' The benders part does not provide interface variables

Public Sub IMaster_VarDefs(vlist() As Variant)

End Sub

Public Sub IMaster_VarVals(dlp As LP, vvars() As Double)

End Sub

' A specialist initialise routine which compiles the master problem interface variables

Public Sub InitVarList(masterlp As LP, subproblp() As LP)
    Dim mpart As IMaster
    
    Set mlp = masterlp
    vmax = mlp.TConsMat.Cupb
    splp = subproblp
    pmax = UBound(splp)
    
    ' dimension for all master vars as interface vars
    
    ReDim mvlist(vmax) As Variant
    ReDim cmvars(vmax) As Double
    ReDim lmvars(vmax) As Double
    ReDim spsame(pmax) As Boolean
    ReDim sprq(pmax) As Long
    ReDim sprc(pmax) As Long
    ReDim spobj(pmax) As Double
    ReDim spiter(pmax) As Long
    ReDim margp(pmax) As Variant
    
    For Each mpart In mparts
        mpart.VarDefs mvlist
    Next mpart
    
End Sub


' Solve benders decomposition

Public Function Solve(schedname As String, subprobcosts() As Double, subprobscale() As Double) As Double
    Dim zlwb As Double, zupb As Double, mvc As Double
    Dim spfail As Boolean, chng As Boolean, recovery As Long
    Dim zmps() As Double
    Dim i As Long, c As Long, q As Long, cut As Long
    Dim iter As Long, sstr As String
    
    spcosts = subprobcosts
    spscale = subprobscale
    
    ReDim spiter(pmax) As Long
    ReDim spsame(pmax) As Boolean
    
    Do
        zlwb = 0#
        spfail = False
        recovery = 0
        
        For i = 0 To pmax
            If Not spsame(i) Then
                sprq(i) = splp(i).SolveLP(c)
                sprc(i) = c
                Debug.Print "Sub-problem Period "; pertab.GetCell(i + 1, "Period"); " rc="; sprq(i)
                spobj(i) = (splp(i).Objective() - spcosts(i)) * spscale(i)
                spiter(i) = spiter(i) + 1
            End If
            
            Select Case sprq(i)
                Case lpOptimum
                    ' No fix up necessary
                    zlwb = zlwb + spobj(i)
                    zprt.ZoneMPS perlp(i), zmps
                    margp(i) = zmps
                
                Case lpInfeasible
                    ' Build infeasibility cut
                    spfail = True
                    cut = InfeasibilityCut(i)
                                        
                Case Else
                    ' Unrecoverable error
                    MsgBox "Day " + CStr(dayn) + " Sched " + schedname + pertab.GetCell(i + 1, "Period") + " subproblem fail"
            End Select
        Next i
        
        If Not spfail Then
        
            berr = zupb - zlwb  ' + mvc
            sstr = "Day " & CStr(dayn) & " Sched " & schedname & " Iter " & CStr(iter) & " Err " & FormatPercent(berr / Abs(zlwb))
            Application.StatusBar = sstr
            Debug.Print sstr
        
            If berr < btol * Abs(zlwb) Then
                Exit Do
            End If

            '*** make an optimality cut
            cut = Optimalitycut(zlwb)
            SavePrices cut, margp
        End If
        
        Do
            q = daylp.SolveLP(c)
            Debug.Print "Master-problem rc="; q
            iter = iter + 1
                        
            Select Case q
                Case lpOptimum
                    Exit Do
                
                Case Else
                    ' Poorly conditioned master problem - reset cuts
                    ResetMaster
                    cut = Optimalitycut(zlwb)
                    SavePrices cut, margp
                    recovery = recovery + 1
                    If recovery > maxrecovery Then
                        Solve = berr
                        Exit Function
                    End If
                    
            End Select
        Loop
        
        zupb = mlp.Objective
        
        chng = UpdateSubProblems()
        
        If Not chng Then
            berr = zupb - zlwb  ' + mvc
            sstr = "Day " & CStr(dayn) & " Sched " & schedname & " Iter " & CStr(iter) & " Err " & FormatPercent(berr / Abs(zlwb))
            Application.StatusBar = sstr
            Debug.Print sstr
        End If
        
    Loop While chng
    
    Solve = zupb - zlwb
End Function

Private Function UpdateSubProblems() As Boolean
    Dim mpart As IMaster
    Dim i As Long
    Dim v As Variant, z As Long, p As Long
    Dim chng As Boolean
        
    lmvars = cmvars                     ' save last vars
        
    For Each mpart In mparts
        mpart.VarVals daylp, cmvars     ' get the new schedule
    Next mpart
    
    For i = 0 To pmax
        spsame(i) = True
    Next i
    
    For i = 0 To vmax
        v = mvlist(i)
        If Not IsEmpty(v) Then
            z = v(0)
            p = v(1)
            
            ' Has store schedule changed?
            If Abs(cmvars(i) - lmvars(i)) > lpEpsilon Then
                spsame(p) = False
                chng = True
                With splp(p)
                    .bvec(z) = .bvec(z) + cmvars(i) - lmvars(i)
                End With
            End If
        End If
    Next i
    UpdateSubProblems = chng
End Function


' This is a specialist update routine which finds and populates a benders cut

Private Function PopulateCut(mag As Double, parmlist() As Double, optimality As Boolean) As Long
    Dim c As Long, cc As Long, i As Long, ii As Long
    Dim ms As Double
    
    For i = 0 To maxcut     ' Find an unused cut or one with the biggest slack
        c = bcc(i).Id
        If mlp.Skip(c) Then
            cc = c
            ii = i
            Exit For
        End If
        If mlp.Slack(c) > ms Then
            cc = c
            ii = i
        End If
    Next i
        
    mlp.Skip(cc) = False
    mlp.bvec(cc) = mag
'    If ii = 0 Then
        mlp.MatAltered          ' Force inverse basis update
'    End If
    
    ' Copy parmlist to cut
    For i = 0 To UBound(parmlist)
        If Abs(parmlist(i)) < lpEpsilon Then
            mlp.TConsMat.Zero cc, i
        Else
            mlp.TConsMat.Cell(cc, i) = parmlist(i)
        End If
    Next i
    
    If optimality Then
        mlp.TConsMat.Cell(cc, wtvar.Id) = 1#
    Else
        mlp.TConsMat.Zero cc, wtvar.Id
    End If
    PopulateCut = ii
End Function

Private Function InfeasibilityCut(sp As Long) As Long
    Dim parms() As Double
    Dim mag As Double, ivc As Double, mvc As Double
    Dim j As Long, v As Variant, z As Long, p As Long
    Dim s As Double
    
    ReDim parms(vmax) As Double
    
    mag = splp(sp).Slack(sprc(sp))      ' magnitude of infeasibility
    ivc = 0#                            ' contribution of master vars
    
    For j = 0 To vmax
        v = mvlist(j)
        If Not IsEmpty(v) Then
            z = v(0)                    ' zone constraint
            p = v(1)                    ' period
            If p = sp Then
                s = splp(sp).ISENS(z)   ' sensitivity to zone demand
                If v(2) * s > lpEpsilon Then
                    parms(j) = -v(2) * s
                    ivc = ivc + cmvars(j) * parms(j)
                End If
            End If
        End If
    Next j
    InfeasibilityCut = PopulateCut(mag - ivc, parms, False)
End Function

Private Function Optimalitycut(zlwb As Double) As Long
    Dim parms() As Double
    Dim mag As Double, ivc As Double, mvc As Double
    Dim i As Long, v As Variant, z As Long, p As Long
    Dim mp As Double
    
    ReDim parms(vmax) As Double
    ivc = 0#
    
    For i = 0 To vmax
        v = mvlist(i)
        If Not IsEmpty(v) Then
            z = v(0)
            p = v(1)
            mp = splp(p).Shadow(z)
            parms(i) = v(2) * mp * spscale(p)
            ivc = ivc + cmvars(i) * mp * spscale(p)
         End If
    Next i
    Optimalitycut = PopulateCut(zlwb - ivc, parms, True)
End Function

Public Sub SavePrices(cut As Long, zoneprices() As Variant)
    zp(cut) = zoneprices
End Sub

Public Sub SPStatus(ByRef iters() As Long, tsobj() As Double)
  
    iters = spiter
    tsobj = spobj
End Sub

Private Sub ResetMaster()
    Dim part As IPart
    
    For Each part In mparts
        part.Initialise mlp, savesmp
    Next part
End Sub
*/

using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public class Benders : IPart, IMaster
    {
        private const int _maxcut = 50;        // Maximum number of bender cuts
        private const int _maxrecovery = 2;    // Maximum retries of bad master 
        private const double _btol = 0.0005;   // 0.05% of objective

        private LP _mlp;             // master problem lp
        private object[][] _mvlist;  // master variable list
        private double[] _cmvars;    // current master variables
        private double[] _lmvars;    // previous master variables
        private bool[] _spsame;      // has subproblem(i)changed?
        private LP[] _splp;          // subproblem lps
        private int[] _sprq;         // subproblem return codes
        private int[] _sprc;         // subproblem return2
        private double[] _spobj;     // supproblem objective result
        private int[] _spiter;       // number of times subproblem solved
        private double[] _spcosts;   // extra sub problem costs
        private double[] _spscale;   // sub problem scaling factor (i.e.number of hours)
        private double[][] _margp;     // marg prices for subproblems
        private int _vmax;           // upb master vars
        private int _pmax;           // upb supproblems
        private LPConsDef[] _bcc;    // bender cuts
        private LPVarDef _wtvar;     // the weight variable
        private double[][][] _zp;    // save zone prices
        private double[,] _savesmp;
        private double[] _wt;        // cut weights

        private string _pname;

        private ModelManager _modelManager;

        private ElsiData _data;
        

        public string PartName { 
            get {
                return _pname;
            }
            set {
                _pname = value;
            }
        }

        public void Build(ModelManager modelManager, LPModel lpm, ElsiPeriod period=ElsiPeriod.Pk)
        {
            int i;

            _modelManager = modelManager;
            _data = modelManager.Data;

            _bcc = new LPConsDef[_maxcut+1];
            
            // initialise first cut as optimality cut with blank data
            _bcc[0] = lpm.ConsDef("cut" + 0.ToString(), false, 0, new object[]{"wtv",1});

            _wtvar = lpm.VarDef("wtv",_bcc[0].name, 1);

            // Remaining cuts empty
            for( i=1;i<=_maxcut;i++) {
                _bcc[i] = lpm.ConsDef("cut" + i.ToString(), false, 0, new object[0]);
            }

        }

        public void Update(LP mlp, ElsiPeriod? period = null)
        {
            // do nothing
        }

        public void SetPhase(LP mlp, int phaseid, object[,] auxdata)
        {
            // do nothing
        }

        public void Initialise(LP mlp, double[,] smp)
        {
            int i, k, v, d;

            _zp = new double[_maxcut+1][][];
            //?? maybe need to copy array??
            if ( smp==null ) {
                _savesmp = null;
            } else {
                _savesmp = Utilities.CopyArray(smp);
            }

            // skip all cuts (nb active cuts remin in basis)
            for(i=0;i<=_maxcut;i++) {
                mlp.SetSkip(_bcc[i].Id, true);
            }

            v = _wtvar.Id;
            k = _bcc[0].Id;
            mlp.EnterBasis(v,k);
        }

        // Output the zone prices
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray)
        {
            int i, j, k, c;
            double[][] p;
            double[] q;
            double chk=0;

            oparray = new object[_data.ZDem.Count+1,_data.Per.Count+1];
            _wt = new double[_maxcut+1];

            for( k=0;k<=_maxcut;k++) {
                c = _bcc[k].Id;
                if ( !mlp.GetSkip(c) && mlp.InBasis(c) ) {
                    _wt[k] = mlp.Shadow(c);
                    chk = chk + _wt[k];
                    p = _zp[k];
                    for(j=0;j<_data.Per.Count;j++) {
                        q = p[j];
                        for(i=0;i<_data.ZDem.Count;i++) {
                            var oa = oparray[i+1,j+1];
                            if ( oa==null ) {
                                oa = (double) 0;
                            }
                            oparray[i+1,j+1] = (double) oa + _wt[k]*q[i];
                        }
                    }
                }
            }
        }

        // The benders part does not provide interface variables
        public void VarDefs(object[] vlist)
        {
        }

        public void VarVals(LP dlp, double[] vvars)
        {
        }

        // A specialist initialise routine which compiles the master problem interface variables
        public void InitVarList(LP masterlp, LP[] subproblp) {
            _mlp = masterlp;
            _vmax = _mlp.TConsMat.Cupb;
            _splp = subproblp;
            _pmax = _splp.Length-1;

            // Dimension for all master vars as interface vars
            _mvlist = new object[_vmax+1][];
            _cmvars = new double[_vmax+1];
            _lmvars = new double[_vmax+1];
            _spsame = new bool[_pmax+1];
            _sprq = new int[_pmax+1];
            _sprc = new int[_pmax+1];
            _spobj = new double[_pmax+1];
            _spiter = new int[_pmax+1];
            _margp = new double[_pmax+1][];

            foreach( IMaster mpart in _modelManager.MParts.Items) {
                mpart.VarDefs(_mvlist);
            }
        }

        // Solve benders decomposition
        public double Solve(string schedname, double[] subprobcosts, double[] subprobscale, bool parallelProcessing = false) {
            double zlwb, zupb=0, mvc;
            bool spfail, chng=false;
            int recovery, i, c=0, q, cut, iter=0;
            double[] zmps;
            string sstr;

            _spcosts = Utilities.CopyArray(subprobcosts);
            _spscale = Utilities.CopyArray(subprobscale);

            _spiter = new int[_pmax+1];
            _spsame = new bool[_pmax+1];

            
            var tasks = new Task[_pmax+1];

            do {
                zlwb = 0;
                spfail = false;
                recovery = 0;

                // Solve slave LPs in parallel
                if (parallelProcessing ) {
                    if ( !_spsame[0]) {
                        tasks[0] = Task.Run(()=>{int cc=0; _sprq[0] = _splp[0].SolveLP(ref cc); _sprc[0]=cc;});
                    }
                    if ( !_spsame[1]) {
                        tasks[1] = Task.Run(()=>{int cc=0; _sprq[1] = _splp[1].SolveLP(ref cc); _sprc[1]=cc;});
                    }
                    if ( !_spsame[2]) {
                        tasks[2] = Task.Run(()=>{int cc=0; _sprq[2] = _splp[2].SolveLP(ref cc); _sprc[2]=cc;});
                    }
                    if ( !_spsame[3]) {
                        tasks[3] = Task.Run(()=>{int cc=0; _sprq[3] = _splp[3].SolveLP(ref cc); _sprc[3]=cc;});
                    }
                    if ( !_spsame[4]) {
                        tasks[4] = Task.Run(()=>{int cc=0; _sprq[4] = _splp[4].SolveLP(ref cc); _sprc[4]=cc;});
                    }
                    Task.WaitAll(tasks);
                }
                

                for( i=0;i<=_pmax;i++) {
                    if ( !_spsame[i] ) {

                        if ( !parallelProcessing ) {
                            _sprq[i] = _splp[i].SolveLP(ref c);
                            _sprc[i] = c;
                        }
                        var objective = _splp[i].Objective();

                        _spobj[i] = (objective - _spcosts[i]) * _spscale[i];
                        #if DEBUG
                            PrintFile.PrintVars("Sub-problem period",_data.Per.GetPeriod(i), "rc", _sprq[i], "objective", objective);
                        #endif

                        _spiter[i] = _spiter[i] + 1;
                    }

                    switch(_sprq[i]) {
                        case LPhdr.lpOptimum:
                            // no fix up necessary
                            zlwb = zlwb + _spobj[i];
                            _modelManager.Zones.ZoneMPS(_modelManager.PerLp[i], out zmps);
                            _margp[i] = Utilities.CopyArray(zmps);
                            break;
                        case LPhdr.lpInfeasible:
                            // Build infeasability cut
                            spfail = true;
                            cut = InfeasibilityCut(i);
                            break;
                        default:
                            // Unrecoverable error
                            throw new Exception($"Day {_data.Day} Sched {schedname} {_data.Per.GetPeriod(i)} subproblem fail");
                    }
                }

                if ( !spfail ) {
                    _modelManager.Berr = zupb - zlwb;
                    sstr = $"Day {_data.Day} Sched {schedname} Iter {iter} Err {_modelManager.Berr/ Math.Abs(zlwb):P}";
                    _modelManager.NewStatusMessage(sstr);
                    #if DEBUG
                        PrintFile.PrintVars("Day", _data.Day, "Sched", schedname, "Iter", iter, "berr", _modelManager.Berr, "zupb", zupb, "zlwb", zlwb, "Err", $"{_modelManager.Berr / Math.Abs(zlwb):P}");
                    #endif

                    if ( _modelManager.Berr < _btol * Math.Abs(zlwb)) {
                        break;
                    }

                    // *** make an optimality cut
                    cut = OptimalityCut(zlwb);
                    SavePrices(cut,_margp);
                }

                do {
                    q = _modelManager.DayLp.SolveLP(ref c);
                    #if DEBUG
                        PrintFile.PrintVars("After daylp.SolveLP q", q);
                    #endif

                    iter = iter + 1;
                    if ( q == LPhdr.lpOptimum) {
                        break;
                    } else {
                        // Poorly conditioned master problem - reset cuts
                        ResetMaster();
                        cut = OptimalityCut(zlwb);
                        SavePrices(cut, _margp);
                        recovery = recovery + 1;
                        if ( recovery > _maxrecovery ) {
                            return _modelManager.Berr;
                        }
                    }
                } while(true);

                zupb = _mlp.Objective();
                #if DEBUG
                    PrintFile.PrintVars("Solved daylp.SolveLP zupb", zupb);
                #endif

                chng = UpdateSubProblems();

                if ( !chng ) {
                    _modelManager.Berr = zupb - zlwb;
                    sstr = $"Day {_data.Day} Sched {schedname} Iter {iter} Err {_modelManager.Berr/ Math.Abs(zlwb):P}";
                    _modelManager.NewStatusMessage(sstr);
                }

            } while( chng );

            return zupb - zlwb;

        }

        private bool UpdateSubProblems() {
            int i, z, p;
            object[] v;
            bool chng=false;

            _lmvars = Utilities.CopyArray(_cmvars);   // save last vars

            foreach( IMaster mpart in _modelManager.MParts.Items) {
                mpart.VarVals(_modelManager.DayLp, _cmvars);  // get the new schedule                
            }

            for ( i=0;i<=_pmax;i++) {
                _spsame[i] = true;
            }

            for( i=0;i<=_vmax;i++) {
                v = _mvlist[i];
                if ( v!=null) {
                    z = (int) v[0];
                    p = (int) v[1];

                    // Has store schedule changed?
                    if ( Math.Abs(_cmvars[i] - _lmvars[i])  > LPhdr.lpEpsilon) {
                        _spsame[p] = false;
                        chng = true;
                        var bvec = _splp[p].GetBvec(z);
                        var cmvars = _cmvars[i];
                        var lmvars = _lmvars[i];
                        var newBvec = bvec + (cmvars -lmvars);
                        _splp[p].SetBvec(z, newBvec );
                    }
                }
            }
            return chng;
        }

        private int PopulateCut(double mag, double[] parmlist, bool optimality) {
            int c, cc=0, i, ii=0;
            double ms=0;

            for(i=0;i<=_maxcut;i++) { // Find an unused cut or onw with the biggest slack
                c = _bcc[i].Id;
                if ( _mlp.GetSkip(c) ) {
                    cc = c;
                    ii = i;
                    break;
                }
                if (_mlp.Slack(c) > ms ) {
                    cc = c;
                    ii = i;
                }
            }

            _mlp.SetSkip(cc, false);
            _mlp.SetBvec(cc,mag);
            _mlp.MatAltered(); // Force inverse basis update

            // Copy parmlist to cut
            for( i=0;i<parmlist.Length;i++) {
                if ( Math.Abs(parmlist[i]) < LPhdr.lpEpsilon) {
                    _mlp.TConsMat.Zero(cc,i);
                } else {
                    _mlp.TConsMat.SetCell(cc, i, parmlist[i]);
                }
            }

            if ( optimality ) {
                _mlp.TConsMat.SetCell(cc, _wtvar.Id, 1);
            } else {
                _mlp.TConsMat.Zero(cc, _wtvar.Id);
            }

            return ii;
        }

        private int InfeasibilityCut(int sp) {
            double[] parms;
            double mag, ivc, mvc, s;
            int j, z, p;
            object[] v;

            parms = new double[_vmax+1];

            mag = _splp[sp].Slack(_sprc[sp]); // Magnitude of infeasibility
            ivc = 0;                        // Contribution of master vars

            for(j=0;j<=_vmax;j++) {
                v =  _mvlist[j];
                if ( v!=null ) {
                    z = (int) v[0];             // Zone constraint
                    p = (int) v[1];             // Period
                    if ( p == sp ) {
                        s = _splp[sp].ISENS(z); // Sensitivity to zone demand
                        if ( ((double) v[2])*s > LPhdr.lpEpsilon ) {
                            parms[j] = -(double) v[2] * s;
                            ivc = ivc + _cmvars[j]*parms[j];
                        }
                    }
                }
            }
            return PopulateCut(mag -ivc, parms, false);
        }

        private int OptimalityCut(double zlwb) {
            double[] parms;
            double mag, ivc, mvc, mp;
            int i, z, p;
            object[] v;

            parms = new double[_vmax+1];
            ivc = 0;

            for(i=0;i<=_vmax;i++) {
                v = _mvlist[i];
                if (v!=null) {
                    z = (int) v[0];
                    p = (int) v[1];
                    mp = _splp[p].Shadow(z);
                    parms[i] = ((int)v[2])* mp * _spscale[p];
                    ivc = ivc + _cmvars[i]*mp*_spscale[p];
                }
            }
            return PopulateCut(zlwb - ivc, parms, true);
        }

        private void SavePrices(int cut, double[][] zoneprices) {
            _zp[cut] = Utilities.CopyArray(zoneprices);
        }

        public void SPStatus(out int[] iters, out double[] tsobj) {
            iters = Utilities.CopyArray(_spiter);
            tsobj = Utilities.CopyArray(_spobj);
        }

        private void ResetMaster() {
            foreach( IPart part in _modelManager.MParts.Items) {
                part.Initialise(_mlp, _savesmp);
            }
        }
    }
}