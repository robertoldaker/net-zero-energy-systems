/*
' Implement generator model part
'

Option Explicit
Option Base 0

Implements IPart


Private pname As String
Private gtab As DTable      ' Main data table
Private data() As Variant   ' Data

' Column positions in gtab
Private c_gnm As Long
Private c_cap As Long
Private c_znm As Long
Private c_co2 As Long
Private c_mp As Long
Private c_bid As Long
Private c_off As Long
Private c_wav As Long
Private c_oav As Long
Private c_tab As Long
Private c_fl1 As Long
Private c_fl2 As Long

Private gupb As Long
Private pupb As Long
Private zupb As Long
Private gvar() As LPVarDef  ' Gen var (if needed by flex constraints)
Private gmc() As LPConsDef  ' Maximum generation constraint
Private gzc() As LPConsDef  ' Minimum generation constraint
Private zgvar() As LPVarDef ' Zone multiseg gen var


Public Property Get IPart_partname() As String
    IPart_partname = pname
End Property

Public Property Let IPart_partname(ByVal rhs As String)
    pname = rhs
End Property

' Get column offsets

Private Sub ColIdentify()
    Dim i As Long
    
    With gtab
        c_gnm = 1
        c_znm = .FindCol("Zone")
        c_cap = .FindCol("Capacity")
        c_co2 = .FindCol("tCO2")
        c_mp = .FindCol("Price")
        c_bid = .FindCol("Bid")
        c_off = .FindCol("Offer")
        c_wav = .FindCol("Wavail")
        c_oav = .FindCol("Oavail")
        c_tab = .FindCol("AvailTable")
        c_fl1 = .FindCol("Flex1")
        c_fl2 = .FindCol("Flex2")
    End With
End Sub

' Build the model part into a LPModel
' dtable is the name of an excel table
' csel is a column selector (e.g. period name)

Public Sub Ipart_Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)
    Dim per As String, i As Long
    Dim gnm As String, znm As String, zid As Long, cst As Double, sz As Double
    Dim dcdef As LPConsDef
    Dim flx1 As Double, flx2 As Double
    Dim flex1c As LPConsDef, flex2c As LPConsDef
    Dim zmo As MO
    Dim ignoreflex As Boolean
    
    Set gtab = dtab
    ColIdentify
    gtab.GetData data
    
    gupb = gtab.RowCount - 1
    pupb = pertab.RowCount - 1
    zupb = ztab.RowCount - 1
    
    ReDim gvar(gupb) As LPVarDef
    ReDim gmc(gupb) As LPConsDef
    ReDim gzc(gupb) As LPConsDef
    ReDim zgvar(zupb) As LPVarDef
    
    For i = 0 To gupb
        gnm = data(i + 1, c_gnm)
        znm = data(i + 1, c_znm)
        zid = zprt.ZoneId(znm) - 1
        If zprt.ZoneId(znm) < 0 Then
            MsgBox "Gen " + gnm + " unknown zone " + znm
        End If

        Set dcdef = zprt.DemConsDef(znm)
        
        sz = data(i + 1, c_cap)     ' Ignore availability in build phase
        cst = data(i + 1, c_mp)
        flx1 = data(i + 1, c_fl1)
        flx2 = data(i + 1, c_fl2)
        ignoreflex = (flex1c Is Nothing) And (flex2c Is Nothing)
                    
        If True Then '  (Not ignoreflex) And flx1 <> 0# And flx2 <> 0# Then
            ' make new specific variable
            Set gvar(i) = lpm.VarDef(gnm & "v", gnm & "zc", cst)
            Set gzc(i) = lpm.ConsDef(gnm & "zc", False, 0#, Array(gnm & "v", 1#))
            Set gmc(i) = lpm.ConsDef(gnm & "mc", False, sz, Array(gnm & "v", -1#))
            ' contributes to zone demand
            dcdef.augment gnm & "v", 1#
            ' contributes to flex constraints
            If Not flex1c Is Nothing Then
                flex1c.augment gnm & "v", flx1
            End If
            If Not flex2c Is Nothing Then
                flex2c.augment gnm & "v", flx2
            End If
        Else
            If zgvar(zid) Is Nothing Then    ' Make a multiseg variable for this zone
                Set zgvar(zid) = lpm.VarDef(znm & "gv", znm & "gzc")
                lpm.ConsDef znm & "gzc", False, 0#, Array(znm & "gv", 1#)
                lpm.ConsDef znm & "gmc", False, 999#, Array(znm & "gv", -1#)
                dcdef.augment znm & "gv", 1#    ' contributes to zone demand
                With zgvar(zid)
                    Set zmo = New MO    ' create a new merit order for this zone
                    Set .MOmgr = zmo
                    .vdc = dcdef.name   ' the demand constraint
                    .vzc = znm & "gzc"  ' the zero constraint
                    .vmc = znm & "gmc"  ' the max constraint
                End With
            End If
            ' add this segment to the multiseg variable
            Set zmo = zgvar(zid).MOmgr
            zmo.Add gnm, cst, sz
        End If
    Next i
End Sub

' Update model parameters in the resulting LP
'

Public Sub Ipart_Update(mlp As LP, Optional csel As Variant)
    Dim i As Long
    Dim gnm As String, znm As String, pnm As Variant
    Dim sz As Double, cst As Double
    Dim per As String
    Dim zmsv As LPMSV, zmo As MO
    Dim season As String
    Dim atab As String, cnm As String, av As Double
    
    per = csel

    gtab.GetData data       ' refresh data
    season = Range("Season").Value
    dayn = Range("DayN").Value
    
    For i = 0 To gupb
        gnm = data(i + 1, c_gnm)
        znm = data(i + 1, c_znm)
        pnm = Split(znm, "_")
        cst = data(i + 1, c_mp)
        
        If season = "W" Then
            av = data(i + 1, c_wav)
        Else
            av = data(i + 1, c_oav)
        End If
        
        atab = data(i + 1, c_tab)
        If atab <> "NA" Then
            cnm = atab & "[[#Headers],[" & pnm(0) & " " & per & "]]"
            av = Range(cnm).Offset(dayn, 0).Value
        End If
        
        sz = data(i + 1, c_cap) * av
        
'        If Not gvar(i) Is Nothing Then  ' a specific gen variable
            mlp.cvec(gvar(i).Id) = cst
            mlp.bvec(gmc(i).Id) = sz
'        Else
'            Set zmsv = mlp.MSVars.Item(znm & "gv")
'            Set zmo = zmsv.mmo
'            zmo.Update gnm, cst, sz
'        End If
    Next i
End Sub

' Set the model ready for a phase of the solution

Public Sub Ipart_SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)

End Sub

' Initialise the LP based on a provisional marginal price

Public Sub Ipart_Initialise(mlp As LP, smp() As Variant)
    Dim gnm As String, znm As String, z As Long
    Dim sz As Double, cst As Double, nm As String
    Dim i As Long, v As Long, c As Long
    Dim zmsv As LPMSV, zmo As MO
    
    ' Initialise gens with individual variables
    
    For i = 0 To gupb
        znm = data(i + 1, c_znm)
        z = zprt.ZoneId(znm) - 1
        
        If Not gvar(i) Is Nothing Then  ' a specific gen variable
            v = gvar(i).Id
            cst = mlp.cvec(v)
            
            If cst <= smp(z, 0) Then
                c = gmc(i).Id
            Else
                c = gzc(i).Id
            End If
            mlp.EnterBasis v, c
        End If
    Next i
    
    ' now initialise zone variables and insert demand constraint
'    For i = 0 To zupb
'        If Not zgvar(i) Is Nothing Then
'            Set zmsv = mlp.MSVars.Item(zgvar(i).name)
'            With zmsv
'                Set zmo = .mmo
'                .pos = zmo.Pschedule(smp(i, 0) * 1#)
'                zmo.PosDetails .pos, nm, cst, sz
'
'                If cst <= smp(i, 0) Then
'                    c = .mcid
'                Else
'                    c = .zcid
'                End If
'                mlp.EnterBasis .vid, c
'            End With
'        End If
'    Next i
End Sub


' Create system or zone merit orders

Public Sub SystemMO(mlp As LP, ByRef zonemo() As MO)
    Dim gnm As String, znm As String
    Dim sz As Double, cst As Double
    Dim i As Long, zid As Long
    Dim zmsv As LPMSV, zmo As MO
       
    For i = 0 To gupb
        gnm = data(i + 1, c_gnm)
        znm = data(i + 1, c_znm)
        zid = zprt.ZoneId(znm) - 1
        
'        If Not gvar(i) Is Nothing Then  ' a specific gen variable
            cst = mlp.cvec(gvar(i).Id)
            sz = mlp.bvec(gmc(i).Id)
'        Else
'            Set zmsv = mlp.MSVars.Item(znm & "gv")
'            Set zmo = zmsv.mmo
'            zmo.Details gnm, cst, sz
'         End If
        
         zonemo(zid).Add gnm, cst, sz
    Next i
End Sub


' Provide outputs

Public Sub Ipart_Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)
    Dim i As Long, d As Long
    Dim gnm As String, znm As String
    Dim zmo As MO
    Dim zmsv As LPMSV
    Dim cst As Double, sz As Double, emr As Double
    
    ReDim oparray(1 To gupb + 1, 1 To 1) As Variant
      
    Select Case dtype
    
    Case d_name
        For i = 0 To gupb
            gnm = data(i + 1, c_gnm)
            oparray(i + 1, 1) = gnm
        Next i
        
    Case d_zone
        For i = 0 To gupb
            znm = data(i + 1, c_znm)
            oparray(i + 1, 1) = znm
        Next i
        
    Case d_sched ' schduled output
        For i = 0 To gupb
'            If Not gvar(i) Is Nothing Then
                oparray(i + 1, 1) = mlp.Slack(gzc(i).Id)
'            Else
'                Set zmsv = mlp.MSVars.Item(znm & "gv")
'                Set zmo = zmsv.mmo
'                zmo.Details gnm, cst, sz
'                d = zmo.Dispatch(gnm, zmsv.pos)
                
'                If d = 0 Then       ' marginal
'                    oparray(i + 1, 1) = mlp.Slack(zmsv.zcid)
'                ElseIf d > 1 Then   ' in-merit
'                    oparray(i + 1, 1) = sz
'                Else                ' out of merit
'                    oparray(i + 1, 1) = 0#
'                End If
'            End If
        Next i
    
    Case d_avail ' gen availabilities
        For i = 0 To gupb
'            If Not gvar(i) Is Nothing Then
                oparray(i + 1, 1) = mlp.bvec(gmc(i).Id)
'            Else
'                Set zmsv = mlp.MSVars.Item(znm & "gv")
'                Set zmo = zmsv.mmo
'                zmo.Details gnm, cst, sz
'                oparray(i + 1, 1) = sz
'            End If
        Next i
        
    Case d_cost ' gen srmcs
        For i = 0 To gupb
            oparray(i + 1, 1) = data(i + 1, c_mp)
        Next i
    
    Case d_cap ' gen capacities
        For i = 0 To gupb
            oparray(i + 1, 1) = data(i + 1, c_cap)
        Next i
        
    Case d_emissions ' rate per hour
        For i = 0 To gupb
            emr = data(i + 1, c_co2)
'            If Not gvar(i) Is Nothing Then
                oparray(i + 1, 1) = mlp.Slack(gzc(i).Id) * emr
'            Else
'                Set zmsv = mlp.MSVars.Item(znm & "gv")
'                Set zmo = zmsv.mmo
'                zmo.Details gnm, cst, sz
'                d = zmo.Dispatch(gnm, zmsv.pos)
                
'                If d = 0 Then       ' marginal
'                    oparray(i + 1, 1) = mlp.Slack(zmsv.zcid) * emr
'                ElseIf d > 1 Then   ' in-merit
'                    oparray(i + 1, 1) = sz * emr
'                Else                ' out of merit
'                    oparray(i + 1, 1) = 0#
'                End If
'            End If
        Next i
        
    Case d_bids
        For i = 0 To gupb
            oparray(i + 1, 1) = data(i + 1, c_bid)
        Next i
        
    Case d_offers
        For i = 0 To gupb
            oparray(i + 1, 1) = data(i + 1, c_off)
        Next i
    
    End Select
End Sub
*/
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{    
    public class Gens : IPart {

        private ModelManager _modelManager;
        private string _pname;
        private ElsiData _data;
        private LPVarDef[] _gvar; // Gen var (if needed by flex constraints)
        private LPConsDef[] _gmc; // Maximum generation constraint
        private LPConsDef[] _gzc; // Minimum generation constraint
        private LPVarDef[] _zgvar; // Zone multiseg gen var

        public string PartName {
            get {
                return _pname;
            }
            set {
                _pname = value;
            }
        }

        public void Build(ModelManager modelManager, LPModel lpm, ElsiPeriod period=ElsiPeriod.Pk ) {
            int i, zid;
            string gnm;
            ElsiMainZone znm;
            double cst, sz;
            double? flx1, flx2;
            LPConsDef dcdef;
            LPConsDef? flex1c=null, flex2c=null;
            bool ignoreflex;

            _modelManager = modelManager;
            _data = modelManager.Data;

            _gvar = new LPVarDef[_data.Gen.Count];
            _gmc = new LPConsDef[_data.Gen.Count];
            _gzc = new LPConsDef[_data.Gen.Count];
            _zgvar = new LPVarDef[_data.ZDem.Count];

            i=0;
            foreach( var g in _data.Gen.Items) {
                gnm = g.GenName;
                znm = g.Zone;
                zid = _modelManager.Zones.ZoneId(znm) - 1;
                if ( _modelManager.Zones.ZoneId(znm) <0 ) {
                    throw new Exception($"Gen {gnm} unknown zone {znm}");
                }

                dcdef = _modelManager.Zones.DemConsDef(znm);

                sz = g.Capacity; // Ignore availability in build phase
                cst = g.Price;
                flx1 = g.Flex1;
                flx2 = g.Flex2;
                ignoreflex = flex1c==null && flex2c==null;
                if ( true ) {
                    // Make new specific variable
                    _gvar[i] = lpm.VarDef(gnm + "v", gnm + "zc", cst);
                    _gzc[i] = lpm.ConsDef(gnm + "zc", false, 0, new object[]{ gnm+"v",1 });
                    _gmc[i] = lpm.ConsDef(gnm + "mc", false, sz, new object[]{ gnm + "v",-1 });
                    // contributions to zone demand
                    dcdef.augment(gnm + "v", 1);
                    // contributes to flex contraints
                    if ( flex1c!=null) {
                        flex1c.augment( gnm + "v", (double) flx1);
                    }
                    if ( flex2c!=null) {
                        flex2c.augment(gnm + "v", (double) flx2);
                    }
                } else {
                    /* 
                    if ( zgvar[zid]==null ) { // make a multiseg variable for this zone
                        zgvar[zid] = lpm.VarDef(znm + "gv", znm + "gzc");
                        lpm.ConsDef(znm + "gzc", false, 0, new object[]{znm+"gv",1});
                        lpm.ConsDef(znm + "gmc", false, 999, new object[]{znm+"gv",-1});
                        dcdef.augment(znm + "gv", 1) // contributes to zone demand
                        zmo = new MO(); // Create a new merit order for this zone
                        zgvar[zid].MOmgr = zmo;
                        zgvar[zid].vdc = dcdef.name;  // the demand contraint
                        zgvar[zid].vzc = znm + "gzc"; // the zero constraint
                        zgvar[zid].vmc = znm + "gmc"; // the max constraint
                    }
                    // add this segment to the multiseg variable
                    zmo = zgvar[zid].MOmgr;
                    zmo.Add(gnm, cst, sz);
                    */
                }
                i++;
            }
        }

        // Update model parameters in the resulting LP
        public void Update(LP mlp, ElsiPeriod? per=null) {
            int i,day;
            ElsiMainZone znm;
            string gnm, season, atab, cnm;
            double sz,cst;
            double? av;
            LPMSV zmsv;
            MO zmo;
            ElsiProfile pro;

            day = _data.Day;
            season = _data.Season;

            #if DEBUG
                PrintFile.PrintVars("Gens");
            #endif 

            i=0;
            foreach( var g in _data.Gen.Items) {
                gnm = g.GenName;
                znm = g.Zone;
                pro = _data.GetProfile(znm);
                cst = g.Price;
                // See if availability via SolarAvail, OnShoreAvail etc.  tables
                av = _data.Availabilities.GetAvailability(g.Type,pro,(ElsiPeriod) per);
                if ( av == null ) {
                    // If not then take from Wavail or Oavail
                    if ( season == "W" ) {
                        av = g.Wavail;
                    } else {
                        av = g.Oavail;
                    }
                }
                //
                sz = g.Capacity * (double) av;
                mlp.SetCvec(_gvar[i].Id, cst);
                mlp.SetBvec(_gmc[i].Id, sz);
                //
                #if DEBUG
                    PrintFile.PrintVars("i", i, "gvar.Id", _gvar[i].Id, "cst", cst, "gmc.Id", _gmc[i].Id, "sz", sz);
                #endif

                //
                i++;
            }
        }

        // Set the model ready for a phase of the solution
        public void SetPhase( LP mlp, int phaseId, object[,] auxdata) {

        }

        // Initialise the LP based on a provisional marginal price
        public void Initialise(LP mlp, double[,] smp) {
            int z, i, v, c;
            double cst;
            ElsiMainZone znm;

            // Initialise gens with individual variables
            i=0;
            foreach( var g in _data.Gen.Items) {
                znm = g.Zone;
                z = _modelManager.Zones.ZoneId(znm) - 1;
                if ( _gvar[i]!=null ) { // A specific gen variable
                    v = _gvar[i].Id;
                    cst = mlp.GetCvec(v);
                    if ( cst <= smp[z,0]) {
                        c = _gmc[i].Id;
                    } else {
                        c = _gzc[i].Id;
                    }
                    mlp.EnterBasis(v,c);
                }
                i++;
            }

        }

        // Create system or zone merit orders
        public void SystemMO(LP mlp, MO[] zonemo) {
            string gnm;
            ElsiMainZone znm;
            double sz, cst;
            int i, zid;

            i=0;
            foreach( var l in _data.Gen.Items) {
                gnm = l.GenName;
                znm = l.Zone;
                zid = _modelManager.Zones.ZoneId(znm)-1;

                cst = mlp.GetCvec(_gvar[i].Id);
                sz = mlp.GetBvec(_gmc[i].Id);
                //
                zonemo[zid].Add(gnm, cst, sz);
                //
                i++;
            }
        }

        // Provide outputs
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray) {
            int i,d;
            string gnm;
            ElsiMainZone znm;
            double cst, sz, emr;

            oparray = new object[_data.Gen.Count+1,2];
            //
            switch (dtype) {
                case ModelConsts.d_name:
                    i=0;
                    foreach(var l in _data.Gen.Items) {
                        gnm = l.GenName;
                        oparray[i+1,1] = gnm;
                        i++;
                    }
                    break;
                case ModelConsts.d_zone:
                    i=0;
                    foreach(var l in _data.Gen.Items) {
                        znm = l.Zone;
                        oparray[i+1,1] = znm;
                        i++;
                    }
                    break;
                case ModelConsts.d_sched:
                    i=0;
                    foreach(var l in _data.Gen.Items) {
                        oparray[i+1,1] = mlp.Slack(_gzc[i].Id);
                        i++;
                    }
                    break;
                case ModelConsts.d_avail:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        oparray[i+1,1] = mlp.GetBvec(_gmc[i].Id);
                        i++;
                    }
                    break;
                case ModelConsts.d_cost:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        oparray[i+1,1] = l.Price;
                        i++;
                    }
                    break;
                case ModelConsts.d_cap:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        oparray[i+1,1] = l.Capacity;
                        i++;
                    }
                    break;
                case ModelConsts.d_emissions:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        emr = l.Emissions;
                        oparray[i+1,1] = mlp.Slack(_gzc[i].Id) * emr;
                        i++;
                    }
                    break;
                case ModelConsts.d_bids:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        oparray[i+1,1] = l.Bid;
                        i++;
                    }
                    break;
                case ModelConsts.d_offers:
                    i=0;
                    foreach( var l in _data.Gen.Items) {
                        oparray[i+1,1] = l.Offer;
                        i++;
                    }
                    break;
            }
        }
    }
}