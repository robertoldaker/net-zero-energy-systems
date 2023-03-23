/* VBA source code
 ' Implement and manage the storage model part
'
' Lewis Dale 2 July 2018


Option Explicit
Option Base 0

Implements IPart
Implements IMaster

Private supb As Long        ' Number of stores -1
Private pupb As Long        ' Number of periods -1

' column positions in data table
Private c_snm As Long
Private c_znm As Long
Private c_gc As Long
Private c_pc As Long
Private c_wav As Long
Private c_oav As Long
Private c_eff As Long
Private c_end As Long
Private c_bid As Long
Private c_off As Long

Private stab As DTable      ' the store data table
Private data() As Variant   ' store data
Private gvar() As LPVarDef  ' gen vars each time step
Private pvar() As LPVarDef  ' pump vars
Private gvzc() As LPConsDef ' gen var zero constraint
Private gvmc() As LPConsDef ' gen var max constraint
Private pvzc() As LPConsDef ' pump var zero constraint
Private pvmc() As LPConsDef ' pump var max constraint
Private snc() As LPConsDef  ' store neutrality constraint
Private zdcd() As LPConsDef ' the zone demand constraint

Private pname As String

Public Property Get IPart_partname() As String
    IPart_partname = pname
End Property

Public Property Let IPart_partname(ByVal rhs As String)
    pname = rhs
End Property

' Get column offsets

Private Sub ColIdentify()
    c_snm = 1
    c_znm = stab.FindCol("Zone")
    c_gc = stab.FindCol("Capacity")
    c_pc = stab.FindCol("MaxPump")
    c_oav = stab.FindCol("Oavail")
    c_wav = stab.FindCol("WAvail")
    c_eff = stab.FindCol("CycleEff")
    c_end = stab.FindCol("Endurance")
    c_bid = stab.FindCol("Bid")
    c_off = stab.FindCol("Offer")
End Sub

' Build the model part

Public Sub Ipart_Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)
    Dim i As Long, j As Long
    Dim hrs As Double, pn As String
    Dim snm As String, znm As String
    Dim gc As Double, pc As Double
    Dim eff As Double
    Dim periods() As Variant
    Dim dcdef As LPConsDef
    
    
    Set stab = dtab
    ColIdentify
    stab.GetData data           ' Get the store data
    supb = stab.RowCount - 1
    pupb = pertab.RowCount - 1

    pertab.GetData periods
    
    ReDim gvar(supb, pupb) As LPVarDef
    ReDim pvar(supb, pupb) As LPVarDef
    ReDim gvzc(supb, pupb) As LPConsDef
    ReDim pvzc(supb, pupb) As LPConsDef
    ReDim gvmc(supb, pupb) As LPConsDef
    ReDim pvmc(supb, pupb) As LPConsDef
    ReDim snc(supb) As LPConsDef
    ReDim zdcd(supb) As LPConsDef
    
    For i = 0 To supb
        snm = data(i + 1, c_snm)
        znm = data(i + 1, c_znm)
        
        gc = data(i + 1, c_gc)          ' Assume 100% availability in build phase
        pc = data(i + 1, c_pc)
        eff = data(i + 1, c_eff)
        
        If zprt.ZoneId(znm) = 0 Then
            MsgBox "Store " + snm + " unknown zone " + znm
        End If
        
        Set zdcd(i) = zprt.DemConsDef(znm) ' used in interface to benders subproblem
                             
        Set snc(i) = lpm.ConsDef(snm + "nc", True, 0#, Array())  ' store neutrality constraint
        
        For j = 0 To pupb
            pn = snm + periods(j + 1, 1)
            hrs = periods(j + 1, 2)
            Set gvar(i, j) = lpm.VarDef(pn + "gv", pn + "gvzc") ' gen & pump vars
            Set pvar(i, j) = lpm.VarDef(pn + "pv", pn + "pvzc")
            Set gvzc(i, j) = lpm.ConsDef(pn + "gvzc", False, 0#, Array(pn + "gv", 1#))      ' zero constraints
            Set pvzc(i, j) = lpm.ConsDef(pn + "pvzc", False, 0#, Array(pn + "pv", 1#))
            Set gvmc(i, j) = lpm.ConsDef(pn + "gvmc", False, gc, Array(pn + "gv", -1#))     ' max constraints
            Set pvmc(i, j) = lpm.ConsDef(pn + "pvmc", False, pc, Array(pn + "pv", -1#))
            snc(i).augment gvar(i, j).name, -hrs        'gen depletes store
            snc(i).augment pvar(i, j).name, eff * hrs   'load fills store inefficiently
        Next j
    Next i
End Sub


' Update the model lp
' updates capacities and hours, enforces endurance constraint

Public Sub Ipart_Update(mlp As LP, Optional csel As Variant)
    Dim i As Long, j As Long
    Dim hrs As Double, pn As String
    Dim gc As Double, pc As Double, av As Double
    Dim en As Double, eff As Double
    Dim periods() As Variant
    Dim maxg As Double, maxp As Double
    Dim season As String
    
    stab.GetData data           ' Get the store data
    pertab.GetData periods      ' get the period data
    season = Range("Season").Value
    
    For i = 0 To supb
        If season = "W" Then
            av = data(i + 1, c_wav)
        Else
            av = data(i + 1, c_oav)
        End If
        gc = data(i + 1, c_gc)
        pc = data(i + 1, c_pc)
        eff = data(i + 1, c_eff)
        en = data(i + 1, c_end)
        
        For j = 0 To pupb
            hrs = periods(j + 1, 2)
            
            If en < hrs Then
                maxg = gc * av * en / hrs  ' scale down gen capacity
            Else
                maxg = gc * av
            End If
            If en * gc < pc * hrs * eff Then
                maxp = gc * av * en / hrs / eff ' scale down pump capacity
            Else
                maxp = pc * av
            End If
            mlp.bvec(gvmc(i, j).Id) = maxg
            mlp.bvec(pvmc(i, j).Id) = maxp
            With mlp.TConsMat
                .Cell(snc(i).Id, gvar(i, j).Id) = -hrs
                .Cell(snc(i).Id, pvar(i, j).Id) = eff * hrs
            End With
        Next j
    Next i
    mlp.MatAltered    ' force new basis inverse
End Sub

Public Sub Ipart_SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)
    ' do nothing
End Sub

' Return LP to all float

Public Sub Ipart_Initialise(mlp As LP, smp() As Variant)
    Dim i As Long, j As Long
      
    For i = 0 To supb
        mlp.EnterBasis gvar(i, 0).Id, snc(i).Id
        mlp.EnterBasis pvar(i, 0).Id, pvzc(i, 0).Id
        For j = 1 To pupb
            mlp.EnterBasis gvar(i, j).Id, gvzc(i, j).Id
            mlp.EnterBasis pvar(i, j).Id, pvzc(i, j).Id
        Next j
    Next i
End Sub

' Extract outputs corresponding to original data table
' Dtype=1 storename, Dtype=2 storeflow , Dtype=2 opportunity prices

Public Sub Ipart_Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)
    Dim i As Long, j As Long
    Dim smp As Double
    Dim snm As String, znm As String
    Dim bidf As Double, offerf As Double, eff As Double
    
    Select Case dtype
    
    Case d_name
        ReDim oparray(1 To 2 * (supb + 1), 1 To 1) As Variant
        For i = 0 To supb
            snm = data(i + 1, c_snm)
            znm = data(i + 1, c_znm)
            oparray(i * 2 + 1, 1) = snm + "G"
            oparray(i * 2 + 2, 1) = snm + "P"
        Next i
        
    Case d_sched ' store flow
        ReDim oparray(1 To 2 * (supb + 1), 1 To pupb + 1)
        For i = 0 To supb
            For j = 0 To pupb
                oparray(i * 2 + 1, j + 1) = mlp.Slack(gvzc(i, j).Id)
                oparray(i * 2 + 2, j + 1) = -mlp.Slack(pvzc(i, j).Id)
            Next j
        Next i
        
    Case d_price ' prices
        ReDim oparray(1 To 2 * (supb + 1), 1 To pupb + 1)
        For i = 0 To supb
            smp = mlp.Shadow(snc(i).Id)
            For j = 0 To pupb
                oparray(i * 2 + 1, j + 1) = smp
                oparray(i * 2 + 2, j + 1) = smp * data(i + 1, c_eff)
            Next j
        Next i
    
    Case d_avail ' store availabilities
        ReDim oparray(1 To 2 * (supb + 1), 1 To pupb + 1)
        For i = 0 To supb
            For j = 0 To pupb
                oparray(2 * i + 1, j + 1) = mlp.bvec(gvmc(i, j).Id)
                oparray(2 * i + 2, j + 1) = -mlp.bvec(pvmc(i, j).Id)
            Next j
        Next i
        
    Case d_cost ' store costs
        ReDim oparray(1 To 2 * (supb + 1), 1 To 1)
        For i = 0 To supb
            oparray(2 * i + 1, 1) = 0#
            oparray(2 * i + 2, 1) = 0#
        Next i
        
    Case d_cap ' store capacities
        ReDim oparray(1 To 2 * (supb + 1), 1 To 1)
        For i = 0 To supb
            oparray(2 * i + 1, 1) = data(i + 1, c_gc)
            oparray(2 * i + 2, 1) = -data(i + 1, c_pc)
        Next i
    
    Case d_offers  ' offer prices
        ReDim oparray(1 To 2 * (supb + 1), 1 To pupb + 1)
        For i = 0 To supb
            offerf = data(i + 1, c_off)
            eff = data(i + 1, c_eff)
            For j = 1 To pupb + 1
                oparray(2 * i + 1, j) = offerf * mlp.Shadow(snc(i).Id)
                oparray(2 * i + 2, j) = offerf * mlp.Shadow(snc(i).Id) * eff
            Next j
        Next i
        
    Case d_bids    ' bid prices
        ReDim oparray(1 To 2 * (supb + 1), 1 To pupb + 1)
        For i = 0 To supb
            bidf = data(i + 1, c_bid)
            eff = data(i + 1, c_eff)
            For j = 1 To pupb + 1
                oparray(2 * i + 1, j) = bidf * mlp.Shadow(snc(i).Id)
                oparray(2 * i + 2, j) = bidf * mlp.Shadow(snc(i).Id) * eff
            Next j
        Next i
    End Select
End Sub


' Identify zoneconstraint, period and multiplier for each LP variable

Public Sub IMaster_VarDefs(vlist() As Variant)
    Dim v As Long
    Dim i As Long, j As Long
       
    For i = 0 To supb
        For j = 0 To pupb
            v = gvar(i, j).Id
            vlist(v) = Array(zdcd(i).Id, j, 1#)
            v = pvar(i, j).Id
            vlist(v) = Array(zdcd(i).Id, j, -1#)
        Next j
    Next i
End Sub

' Return the pump schedule

Public Sub IMaster_VarVals(dlp As LP, vvars() As Double)
    Dim i As Long, j As Long
    
    For i = 0 To supb
        For j = 0 To pupb
            vvars(gvar(i, j).Id) = dlp.Slack(gvzc(i, j).Id)
            vvars(pvar(i, j).Id) = -dlp.Slack(pvzc(i, j).Id)
        Next j
    Next i
End Sub


*/

using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public class Stores : IPart, IMaster {

        private ModelManager _modelManager;
        private string _pname;
        private ElsiData _data;

        private int _supb; // Number of stores - 1
        private int _pupb; // Number of periods - 1

        private LPVarDef[,] _gvar;    // gen vars each time step
        private LPVarDef[,] _pvar;    // pump vars
        private LPConsDef[,] _gvzc;   // gen var zero constraint
        private LPConsDef[,] _gvmc;   // gen var max constraint
        private LPConsDef[,] _pvzc;   // pump var zero constraint
        private LPConsDef[,] _pvmc;   // pump var max constraint
        private LPConsDef[] _snc;    // store neutrality constraint
        private LPConsDef[] _zdcd;   // the zone demand constraint

        public Stores() {
            
        }

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
            int i, j;
            double hrs, gc, pc, eff;
            string pn, snm;
            ElsiMainZone znm;

            _modelManager = modelManager;
            _data = modelManager.Data;

            _supb = _data.Store.Count - 1;
            _pupb = _data.Per.Count -1;

            _gvar = new LPVarDef[_supb+1,_pupb+1];
            _pvar = new LPVarDef[_supb+1,_pupb+1];
            _gvzc = new LPConsDef[_supb+1,_pupb+1];
            _pvzc = new LPConsDef[_supb+1,_pupb+1];
            _gvmc = new LPConsDef[_supb+1,_pupb+1];
            _pvmc = new LPConsDef[_supb+1,_pupb+1];
            _snc = new LPConsDef[_supb+1];
            _zdcd = new LPConsDef[_supb+1];

            i=0;
            foreach( var s in _data.Store.Items ) {
                snm = s.StoreName;
                znm = s.Zone;

                gc = s.Capacity;
                pc = s.MaxPump;
                eff = s.CycleEff;

                if ( _modelManager.Zones.ZoneId(znm) == 0 ) {
                    throw new Exception($"Store {snm} unknown zone {znm}");
                }

                _zdcd[i] = _modelManager.Zones.DemConsDef(znm); // used in interface to benders sub problem

                _snc[i] = lpm.ConsDef(snm + "nc", true, 0, new object[0]); // Store neutrality constraint

                j=0;
                foreach( var p in _data.Per.Items ) {
                    pn = snm + p.Period.ToString();
                    hrs = p.Hours;
                    _gvar[i,j] = lpm.VarDef(pn + "gv", pn + "gvzc");  // gen & pump vars
                    _pvar[i,j] = lpm.VarDef(pn + "pv", pn + "pvzc");
                    _gvzc[i,j] = lpm.ConsDef(pn + "gvzc", false, 0, new object[2] {pn + "gv", 1});   // zero cnstraints
                    _pvzc[i,j] = lpm.ConsDef(pn + "pvzc", false, 0, new object[2] {pn + "pv", 1});
                    _gvmc[i,j] = lpm.ConsDef(pn + "gvmc", false, gc, new object[2] {pn + "gv", -1}); // max constraints
                    _pvmc[i,j] = lpm.ConsDef(pn + "pvmc", false, pc, new object[2] {pn + "pv", -1});
                    _snc[i].augment(_gvar[i,j].name, -hrs);      // gen depletes store
                    _snc[i].augment(_pvar[i,j].name, eff * hrs); // load fills store inefficiently
                    j++;
                }
                i++;
            }

        }

        // Update the model lp
        // updates capacities and hours, enforces endurance contraint
        public void Update(LP mlp, ElsiPeriod? period = null)
        {
            int i, j;
            double hrs, gc, pc, av, en, eff, maxg, maxp;

            #if DEBUG
                PrintFile.PrintVars("Stores update");
                PrintFile.PrintVars("season", _data.Season);
            #endif

            i=0;
            foreach( var s in _data.Store.Items ) {
                if ( _data.Season == "W" ) {
                    av = s.Wavail;
                } else {
                    av = s.Oavail;
                }
                gc = s.Capacity;
                pc = s.MaxPump;
                eff = s.CycleEff;
                en = s.Endurance;

                #if DEBUG
                    PrintFile.PrintVars("av", av, "gc", gc, "pc", pc, "eff", eff, "en", en);
                #endif
        
                j=0;
                foreach( var p in _data.Per.Items) {
                    hrs = p.Hours;

                    if ( en < hrs ) {
                        maxg = gc * av * en /hrs; // scale down gen capacity
                    } else {
                        maxg = gc * av;
                    }
                    if ( (en * gc) < (pc * hrs * eff) ) {
                        maxp = gc * av * en / hrs / eff; // scale down pump capcity
                    } else {
                        maxp = pc * av;
                    }
                    mlp.SetBvec(_gvmc[i,j].Id, maxg);
                    mlp.SetBvec(_pvmc[i,j].Id, maxp);
                    mlp.TConsMat.SetCell(_snc[i].Id, _gvar[i,j].Id, -hrs);
                    mlp.TConsMat.SetCell(_snc[i].Id, _pvar[i,j].Id, eff * hrs);
                    
                    #if DEBUG
                        PrintFile.PrintVars("i", i, "j", j, "gvmc.Id", _gvmc[i, j].Id, "maxg", maxg, "pvmc.Id", _pvmc[i, j].Id, "maxp", maxp);
                        PrintFile.PrintVars("i", i, "j", j, "snc.Id", _snc[i].Id, "gvar.Id", _gvar[i, j].Id, "-hrs", -hrs, "pvar.Id", _pvar[i, j].Id, "eff*hrs", eff * hrs);
                    #endif

                    j++;
                }
                i++;
            }
            mlp.MatAltered(); // force new basis inverse

        }

        public void SetPhase(LP mlp, int phaseid, object[,] auxdata)
        {
            // do nothing
        }

        // Return LP to all float
        public void Initialise(LP mlp, double[,] smp)
        {
            int i,j;

            i=0;
            foreach( var s in _data.Store.Items) {
                mlp.EnterBasis(_gvar[i,0].Id, _snc[i].Id);
                mlp.EnterBasis(_pvar[i,0].Id, _pvzc[i,0].Id);
                for(j=1;j<_data.Per.Count;j++) {
                    mlp.EnterBasis(_gvar[i,j].Id, _gvzc[i,j].Id);
                    mlp.EnterBasis(_pvar[i,j].Id, _pvzc[i,j].Id);
                }
                i++;
            }
        }


        // Extract outputs corresponding to original data table
        // Dtype=1 storename, Dtype=2 streflow, Dtype=3 opportunity prices
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray)
        {
            int i, j;
            double smp, bidf, offerf, eff;
            string snm;
            ElsiMainZone znm;

            switch (dtype) {
                case ModelConsts.d_name:
                    oparray = new object[2*(_supb+1)+1,2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        snm = s.StoreName;
                        znm = s.Zone;
                        oparray[i*2 + 1, 1] = snm + "G";
                        oparray[i*2 + 2, 1] = snm + "P";
                        i++;
                    }
                    break;  
                case ModelConsts.d_sched: // store flow
                    oparray = new object[2*(_supb+1)+1,_pupb+2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        j=0;
                        foreach( var p in _data.Per.Items) {
                            oparray[i*2 + 1, j+1] = mlp.Slack(_gvzc[i,j].Id);
                            oparray[i*2 + 2, j+1] = -mlp.Slack(_pvzc[i,j].Id);
                            j++;
                        }
                        i++;
                    }                    
                    break;
                case ModelConsts.d_price: // prices
                    oparray = new object[2*(_supb+1)+1,_pupb+2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        smp = mlp.Shadow(_snc[i].Id);
                        j=0;
                        foreach( var p in _data.Per.Items) {
                            oparray[i*2 + 1, j+1] = smp;
                            oparray[i*2 + 2, j+1] = smp * s.CycleEff;
                            j++;
                        }
                        i++;
                    }                    
                    
                    break;
                case ModelConsts.d_avail: // stroe availabilities
                    oparray = new object[2*(_supb+1)+1,_pupb+2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        j=0;
                        foreach( var p in _data.Per.Items) {
                            oparray[i*2 + 1, j+1] = mlp.GetBvec(_gvmc[i,j].Id);
                            oparray[i*2 + 2, j+1] = -mlp.GetBvec(_pvmc[i,j].Id);
                            j++;
                        }
                        i++;
                    }                                       
                    break;
                case ModelConsts.d_cost: // store costs
                    oparray = new object[2*(_supb+1)+1,2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        oparray[i*2 + 1, 1] = 0;
                        oparray[i*2 + 2, 1] = 0;
                        i++;
                    }                    
                    break;
                case ModelConsts.d_cap: // store capacities
                    oparray = new object[2*(_supb+1)+1,2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        oparray[i*2 + 1, 1] = s.Capacity;
                        oparray[i*2 + 2, 1] = -s.MaxPump;
                        i++;
                    }                    
                    break;
                case ModelConsts.d_offers: // offer prices
                    oparray = new object[2*(_supb+1)+1,_pupb+2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        offerf = s.Offer;
                        eff = s.CycleEff;
                        j=1;
                        foreach( var p in _data.Per.Items) {
                            oparray[i*2 + 1, j] = offerf * mlp.Shadow(_snc[i].Id);
                            oparray[i*2 + 2, j] = offerf * mlp.Shadow(_snc[i].Id) * eff;
                            j++;
                        }
                        i++;
                    }                                                           
                    break;
                case ModelConsts.d_bids: // bid prices
                    oparray = new object[2*(_supb+1)+1,_pupb+2];
                    i=0;
                    foreach( var s in _data.Store.Items) {
                        bidf = s.Bid;
                        eff = s.CycleEff;
                        j=1;
                        foreach( var p in _data.Per.Items) {
                            oparray[i*2 + 1, j] = bidf * mlp.Shadow(_snc[i].Id);
                            oparray[i*2 + 2, j] = bidf * mlp.Shadow(_snc[i].Id) * eff;
                            j++;
                        }
                        i++;
                    }                                                                               
                    break;
                default:
                    oparray = new object[0,0];
                    break;
            }
        }

        // Identify zoneconstraint, period and multiplier for each LP variable
        public void VarDefs(object[] vlist)
        {
            int v, i, j;
            for( i=0;i<=_supb;i++) {
                for( j=0;j<=_pupb;j++) {
                    v = _gvar[i,j].Id;
                    vlist[v] = new object[3] { _zdcd[i].Id, j, 1};
                    v = _pvar[i,j].Id;
                    vlist[v] = new object[3] { _zdcd[i].Id, j, -1};
                }
            }
        }

        public void VarVals(LP dlp, double[] vvars)
        {
            int i, j;
            for( i=0;i<=_supb;i++) {
                for( j=0;j<=_pupb;j++) {
                    vvars[_gvar[i,j].Id] = dlp.Slack(_gvzc[i,j].Id);
                    vvars[_pvar[i,j].Id] = -dlp.Slack(_pvzc[i,j].Id);
                }
            }
        }
    }
}