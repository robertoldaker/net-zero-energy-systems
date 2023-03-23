/*
' Implement Link2 model part (for multiple links)
' Each Link couples 2 existing zones bidirectionally
'
' Lewis Dale 15 June 2018

Option Explicit
Option Base 0

Implements IPart

Private lupb As Long            'number of links -1

' column positions in data table
Private c_l2nm As Long          'link name
Private c_fnm As Long           'from zone name
Private c_tnm As Long           'to zone name
Private c_fcap As Long          'forward capacity
Private c_tcap As Long          'reverse capacity
Private c_loss As Long          'link loss
Private c_mkt As Long           'mkt resolved?
Private c_itf As Long           'import tariff from zone
Private c_itt As Long           'import tariff to zone
Private c_btf As Long           'bal tariff from
Private c_btt As Long           'bal tariff to

Private l2tab As DTable         'data table
Private data() As Variant

Private l2nc() As LPConsDef     'Neutrality constraint for link
Private dfnc() As LPConsDef     'Demand neutrality constraints sending zone
Private dtnc() As LPConsDef     'Demand neutrality constraints receiving zone
Private dfivar() As LPVarDef    'From import var
Private dfevar() As LPVarDef    'From export var
Private dtivar() As LPVarDef    'To import var
Private dtevar() As LPVarDef    'To export var
Private dfizc() As LPConsDef
Private dfezc() As LPConsDef
Private dtizc() As LPConsDef
Private dtezc() As LPConsDef
Private dfemc() As LPConsDef    'Max consstraint on sending end only (biggest due to losses)
Private dtemc() As LPConsDef    'Max constraint on reverse sending end

Private zfr() As Long          'Zone row numbers
Private ztr() As Long

Private pname As String


Public Property Get IPart_partname() As String
    IPart_partname = pname
End Property

Public Property Let IPart_partname(ByVal rhs As String)
    pname = rhs
End Property

Private Sub ColIdentify()
    With l2tab
        c_l2nm = .FindCol("LinkName")          'link name
        c_fnm = .FindCol("From")               'from zone name
        c_tnm = .FindCol("To")                 'to zone name
        c_fcap = .FindCol("Capacity")          'forward capacity
        c_tcap = .FindCol("RevCap")            'reverse capacity
        c_loss = .FindCol("Loss")              'link loss
        c_mkt = .FindCol("Market")             'mkt resolved?
        c_itf = .FindCol("ITF")                'from zone import tariff
        c_itt = .FindCol("ITT")                'to zone import tariff
        c_btf = .FindCol("BTF")                'from zone import balance tariff
        c_btt = .FindCol("BTT")                'to zone import balance tariff
    End With
End Sub

' Build the model part
' Data(1..n, 1..5) is an Array of linkname, fromzone, tozone, fwdavail, backavail, loss factor, upcst, dcost, upprem, dprem

Public Sub Ipart_Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)
    Dim i As Long, j As Long
    Dim l2nm As String, fzn As String, tzn As String
    Dim capf As Double, capt As Double, loss As Double
    Dim itf As Double, itt As Double
    
    Set l2tab = dtab
    lupb = l2tab.RowCount - 1
    ColIdentify
    l2tab.GetData data
    
    ReDim l2nc(lupb) As LPConsDef       ' Link neutrality constraint
    ReDim dfnc(lupb) As LPConsDef       ' Zone neutrality constraints
    ReDim dtnc(lupb) As LPConsDef
    ReDim dfivar(lupb) As LPVarDef      ' From zone import & export variables
    ReDim dfevar(lupb) As LPVarDef
    ReDim dtivar(lupb) As LPVarDef      ' To zone variables
    ReDim dtevar(lupb) As LPVarDef
    ReDim dfizc(lupb) As LPConsDef      ' From & to zone var zero constraints
    ReDim dfezc(lupb) As LPConsDef
    ReDim dtizc(lupb) As LPConsDef
    ReDim dtezc(lupb) As LPConsDef
    ReDim dfemc(lupb) As LPConsDef      'Max export constraints (on sending end only because larger than import)
    ReDim dtemc(lupb) As LPConsDef
    
    ReDim zfr(lupb) As Long
    ReDim ztr(lupb) As Long
    
    For i = 0 To lupb
        l2nm = data(i + 1, c_l2nm)
        fzn = data(i + 1, c_fnm)
        tzn = data(i + 1, c_tnm)
        capf = data(i + 1, c_fcap)
        capt = data(i + 1, c_tcap)
        loss = data(i + 1, c_loss)
        
        itf = data(i + 1, c_itf)
        itt = data(i + 1, c_itt)
        
        If zprt.ZoneId(fzn) = 0 Then
            MsgBox "Link " + l2nm + " Can't find zone " + fzn
        End If
        If zprt.ZoneId(tzn) = 0 Then
            MsgBox "Link " + l2nm + " Can't find zone " + tzn
        End If
          
        zfr(i) = zprt.ZoneId(fzn) - 1
        ztr(i) = zprt.ZoneId(tzn) - 1
        
        Set l2nc(i) = lpm.ConsDef(l2nm + "nc", True, 0#, Array()) 'The link neutrality constraint
        Set dfnc(i) = zprt.DemConsDef(fzn)  ' from zone demand constraint
        Set dtnc(i) = zprt.DemConsDef(tzn)  ' to zone demand constraint
        
        Set dfivar(i) = lpm.VarDef(l2nm + "fiv", l2nm + "fizc", itf)
        Set dfevar(i) = lpm.VarDef(l2nm + "fev", l2nm + "fezc", 0#)
        Set dtivar(i) = lpm.VarDef(l2nm + "tiv", l2nm + "tizc", itt)
        Set dtevar(i) = lpm.VarDef(l2nm + "tev", l2nm + "tezc", 0#)
        dfnc(i).augment l2nm + "fiv", 1#                              'import var meets demand in to zone
        dfnc(i).augment l2nm + "fev", -1#
        dtnc(i).augment l2nm + "tiv", 1#                              'import var meets demand in to zone
        dtnc(i).augment l2nm + "tev", -1#
        l2nc(i).augment l2nm + "fiv", -1#                             'import vars reduce link power
        l2nc(i).augment l2nm + "fev", 1# - loss
        l2nc(i).augment l2nm + "tiv", -1#                             'import vars reduce link power
        l2nc(i).augment l2nm + "tev", 1# - loss
        Set dfizc(i) = lpm.ConsDef(l2nm + "fizc", False, 0#, Array(l2nm + "fiv", 1#))
        Set dfezc(i) = lpm.ConsDef(l2nm + "fezc", False, 0#, Array(l2nm + "fev", 1#))
        Set dtizc(i) = lpm.ConsDef(l2nm + "tizc", False, 0#, Array(l2nm + "tiv", 1#))
        Set dtezc(i) = lpm.ConsDef(l2nm + "tezc", False, 0#, Array(l2nm + "tev", 1#))
        Set dfemc(i) = lpm.ConsDef(l2nm + "femc", False, capf, Array(l2nm + "fev", -1#))
        Set dtemc(i) = lpm.ConsDef(l2nm + "temc", False, capt, Array(l2nm + "tev", -1#))
    Next i
End Sub

'
' Update the resulting model capacities & costs
'

Public Sub Ipart_Update(mlp As LP, Optional csel As Variant)
    Dim i As Long, j As Long
    Dim capf As Double, capt As Double, loss As Double
    Dim itf As Double, itt As Double
    
    l2tab.GetData data      ' refresh data
    
    For i = 0 To lupb
        capf = data(i + 1, c_fcap)
        capt = data(i + 1, c_tcap)
        loss = data(i + 1, c_loss)
        
        itf = data(i + 1, c_itf)
        itt = data(i + 1, c_itt)
        
        mlp.cvec(dfivar(i).Id) = itf    ' set tariffs
        mlp.cvec(dtivar(i).Id) = itt
        mlp.bvec(dfemc(i).Id) = capf    ' set capacities
        mlp.bvec(dtemc(i).Id) = capt
        mlp.evec(dfnc(i).Id) = 0#        ' no demand adders
        mlp.evec(dtnc(i).Id) = 0#
        
        ' Skip link constraints that are not market resolved
        If Not data(i + 1, c_mkt) Then
            mlp.Skip(dfemc(i).Id) = True
            mlp.Skip(dtemc(i).Id) = True
        End If
    Next i
End Sub

' Set run type
' auxdata(0..lupb, 0..1) gives baseflows at from and to ends

Public Sub Ipart_SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)
    Dim i As Long, j As Long

    If phaseid = 0 Then     ' Market phase
    
        Ipart_Update mlp    ' Reset links
    
    Else                    ' Balance phase
        For i = 0 To lupb
            If data(i + 1, c_mkt) Then
                auxdata(i, 0) = mlp.Slack(dfizc(i).Id) - mlp.Slack(dfezc(i).Id)
                auxdata(i, 1) = mlp.Slack(dtizc(i).Id) - mlp.Slack(dtezc(i).Id)
                mlp.cvec(dfivar(i).Id) = data(i + 1, c_itf) + data(i + 1, c_btf) ' set tariffs and premia
                mlp.cvec(dtivar(i).Id) = data(i + 1, c_itt) + data(i + 1, c_btt)
                mlp.bvec(dfemc(i).Id) = data(i + 1, c_fcap) + auxdata(i, 0)    ' set capacities less export
                mlp.bvec(dtemc(i).Id) = data(i + 1, c_tcap) + auxdata(i, 1)
                mlp.evec(dfnc(i).Id) = mlp.evec(dfnc(i).Id) + auxdata(i, 0) ' adjust demands
                mlp.evec(dtnc(i).Id) = mlp.evec(dtnc(i).Id) + auxdata(i, 1)
            Else
                mlp.Skip(dfemc(i).Id) = False
                mlp.Skip(dtemc(i).Id) = False
            End If
        Next i
    End If
End Sub

' Initialise the resulting model

Public Sub Ipart_Initialise(mlp As LP, smp() As Variant)
    Dim i As Long, fz As Long, tz As Long
    
    For i = 0 To lupb
        fz = zfr(i)
        tz = ztr(i)
        If smp(fz, 1) <= smp(tz, 1) Then
            mlp.EnterBasis dfivar(i).Id, dfizc(i).Id
            mlp.EnterBasis dtevar(i).Id, dtezc(i).Id
            If mlp.Skip(dfemc(i).Id) Then
                mlp.EnterBasis dfevar(i).Id, dfezc(i).Id
            Else
                mlp.EnterBasis dfevar(i).Id, dfemc(i).Id
            End If
            mlp.EnterBasis dtivar(i).Id, l2nc(i).Id
        Else
            mlp.EnterBasis dfivar(i).Id, l2nc(i).Id
            If mlp.Skip(dtemc(i).Id) Then
                mlp.EnterBasis dtevar(i).Id, dtezc(i).Id
            Else
                mlp.EnterBasis dtevar(i).Id, dtemc(i).Id
            End If
            mlp.EnterBasis dfevar(i).Id, dfezc(i).Id
            mlp.EnterBasis dtivar(i).Id, dtizc(i).Id
        End If
    Next i
End Sub

' Get the flows amd prices for market managed links ready for subsequent balancing phase

Public Sub GetFlows(mlp As LP, margp() As Variant, ByRef auxdata() As Variant)
    Dim i As Long, per As Long
    
    ReDim auxdata(lupb, 4) As Variant

    per = mlp.Id    ' Identify period
    
    For i = 0 To lupb
        If data(i + 1, c_mkt) Then
            auxdata(i, 0) = mlp.Slack(dfizc(i).Id) - mlp.Slack(dfezc(i).Id)
            auxdata(i, 1) = mlp.Slack(dtizc(i).Id) - mlp.Slack(dtezc(i).Id)
            auxdata(i, 2) = margp(zfr(i) + 1, per + 1)
            auxdata(i, 3) = margp(ztr(i) + 1, per + 1)
            auxdata(i, 4) = mlp.cvec(dfivar(i).Id) * mlp.Slack(dfizc(i).Id) + mlp.cvec(dtivar(i).Id) * mlp.Slack(dtizc(i).Id)
        End If
    Next i
End Sub

' Extract outputs corresponding to original data table
'

Public Sub Ipart_Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)
    Dim i As Long, per As Long, loss As Double
    Dim fp As Double, tp As Double
    Dim margp() As Double
    
    ReDim oparray(1 To lupb * 2 + 2, 1 To 1) As Variant
    
    Select Case dtype
        
    Case d_name
        For i = 0 To lupb
            oparray(i * 2 + 1, 1) = data(i + 1, c_fnm) + ":" + data(i + 1, c_l2nm)
            oparray(i * 2 + 2, 1) = data(i + 1, c_tnm) + ":" + data(i + 1, c_l2nm)
        Next i
            
    Case d_sched
        For i = 0 To lupb
            oparray(i * 2 + 1, 1) = mlp.Slack(dfizc(i).Id) - mlp.Slack(dfezc(i).Id) + auxdata(i, 0)
            oparray(i * 2 + 2, 1) = mlp.Slack(dtizc(i).Id) - mlp.Slack(dtezc(i).Id) + auxdata(i, 1)
        Next i
        
    Case d_price, d_offers, d_bids    ' auxdata are zone marginal prices
        per = mlp.Id
        For i = 0 To lupb
            loss = 1# + data(i + 1, c_loss)
            fp = auxdata(zfr(i) + 1, per + 1)
            tp = auxdata(ztr(i) + 1, per + 1)
            
            If fp > tp Then ' from-end importing
                ' from-end production cost = to-end (exporting price) plus loss plus import tariff
                oparray(i * 2 + 1, 1) = tp * loss + mlp.cvec(dfivar(i).Id)
                ' to-end production cost = from-end (importing price) less import tariff less loss
                oparray(i * 2 + 2, 1) = (fp - mlp.cvec(dfivar(i).Id)) / loss
            Else    ' to-end importing
                ' from-end production cost = to-end importing price less remote import tariff
                oparray(i * 2 + 1, 1) = (tp - mlp.cvec(dtivar(i).Id)) / loss
                ' to-end production cost = from-end exporting price plus import tariff
                oparray(i * 2 + 2, 1) = fp * loss + mlp.cvec(dtivar(i).Id)
            End If
        Next i
     
    Case d_avail
        For i = 0 To lupb
            oparray(i * 2 + 1, 1) = data(i + 1, c_fcap)
            oparray(i * 2 + 2, 1) = data(i + 1, c_tcap)
        Next i
            
    Case d_cost
        For i = 0 To lupb
            oparray(i * 2 + 1, 1) = mlp.cvec(dfivar(i).Id)
            oparray(i * 2 + 2, 1) = mlp.cvec(dtivar(i).Id)
        Next i
            
    Case d_balcost
        For i = 0 To lupb
            oparray(i * 2 + 1, 1) = data(i + 1, c_itf) + data(i + 1, c_btf)
            oparray(i * 2 + 2, 1) = data(i + 1, c_itt) + data(i + 1, c_btt)
        Next i
    End Select
End Sub

' Decide how many market price areas

Public Sub MarketAreas(zmembers() As Long)
    Dim i As Long, zupb As Long
    Dim chng As Boolean
    
    zupb = ztab.RowCount - 1
    ReDim zmembers(zupb) As Long
    
    For i = 0 To zupb
        zmembers(i) = i     ' start by assuming all zones are their own price area
    Next i
    
    Do
        chng = False
        For i = 0 To lupb
            If Not data(i + 1, c_mkt) Then
                If zmembers(zfr(i)) < zmembers(ztr(i)) Then
                    zmembers(ztr(i)) = zmembers(zfr(i))
                    chng = True
                ElseIf zmembers(zfr(i)) > zmembers(ztr(i)) Then
                    zmembers(zfr(i)) = zmembers(ztr(i))
                    chng = True
                End If
            End If
        Next i
    Loop While chng
End Sub

*/

using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public class Link2 : IPart {

        private LPConsDef[] _l2nc; // Neutrality constraint for link
        private LPConsDef?[] _dfnc; // Demand neutrality constraints sending zone
        private LPConsDef?[] _dtnc; // Demand neutrality constraints receiving zone
        private LPVarDef[] _dfivar;    // From import var
        private LPVarDef[] _dfevar;    // From export var
        private LPVarDef[] _dtivar;    // To import var
        private LPVarDef[] _dtevar;    // To export var
        private LPConsDef[] _dfizc;
        private LPConsDef[] _dfezc;
        private LPConsDef[] _dtizc;
        private LPConsDef[] _dtezc;
        private LPConsDef[] _dfemc;    // Max consstraint on sending end only (biggest due to losses)
        private LPConsDef[] _dtemc;    // Max constraint on reverse sending end

        private int[] _zfr;
        private int[] _ztr;

        private string _pname;
        private ModelManager _modelManager;
        private ElsiData _data; 

        public Link2() {

        }

        public string PartName {
            get {
                return _pname;
            }
            set {
                _pname = value;
            }
        }

        public void Build(ModelManager modelManager, LPModel lpm, ElsiPeriod period=ElsiPeriod.Pk) {
            int i,j;
            string l2nm;
            ElsiMainZone fzn, tzn;
            double capf, capt, loss;
            double itf, itt;
            int fromZId, toZId;

            _modelManager = modelManager;
            _data = modelManager.Data;

            var lupb = _data.Links.Count;
            _l2nc = new LPConsDef[lupb]; // Link neutrality constraint
            _dfnc = new LPConsDef[lupb]; // Zone neutrality constraints
            _dtnc = new LPConsDef[lupb];
            _dfivar = new LPVarDef[lupb]; // From zone import & export variables
            _dfevar = new LPVarDef[lupb];
            _dtivar = new LPVarDef[lupb]; // To zone variables
            _dtevar = new LPVarDef[lupb];
            _dfizc = new LPConsDef[lupb]; // From & to zone var zero constraints
            _dfezc = new LPConsDef[lupb];
            _dtizc = new LPConsDef[lupb];
            _dtezc = new LPConsDef[lupb];
            _dfemc = new LPConsDef[lupb]; // Max export constraints (on sending and only becuase larger than import)
            _dtemc = new LPConsDef[lupb];

            // 
            _zfr = new int[lupb];
            _ztr = new int[lupb];

            //
            i=0;
            foreach( var l in _data.Links.Rows) {
                l2nm = l.Name;
                fzn = l.FromZone;
                tzn = l.ToZone;
                capf = l.Capacity;
                capt = l.RevCap;
                loss = l.Loss;
                //
                itf = l.ITF;
                itt = l.ITT;
                //
                fromZId = _modelManager.Zones.ZoneId(fzn);
                if (  fromZId== 0 ) {
                    throw new Exception($"Link {l2nm} Can't find zone {fzn}");
                }
                toZId = _modelManager.Zones.ZoneId(tzn);
                if ( toZId == 0 ) {
                    throw new Exception($"Link {l2nm} Can't find zone {tzn}");
                }
                //
                _zfr[i] = fromZId - 1;
                _ztr[i] = toZId - 1;
                //
                _l2nc[i] = lpm.ConsDef(l2nm + "nc", true, 0, new object[0]); // The link neutrality contraint
                _dfnc[i] = _modelManager.Zones.DemConsDef(fzn); // From zone demand contraint
                _dtnc[i] = _modelManager.Zones.DemConsDef(tzn); // To zone demand contraint

                _dfivar[i] = lpm.VarDef(l2nm + "fiv", l2nm + "fizc", itf);
                _dfevar[i] = lpm.VarDef(l2nm + "fev", l2nm + "fezc", 0);
                _dtivar[i] = lpm.VarDef(l2nm + "tiv", l2nm + "tizc", itt);
                _dtevar[i] = lpm.VarDef(l2nm + "tev", l2nm + "tezc", 0);
                _dfnc[i].augment(l2nm + "fiv",  1);         // import var meets demand in to zone
                _dfnc[i].augment(l2nm + "fev", -1);          
                _dtnc[i].augment(l2nm + "tiv",  1);        // import var meets demand in to zone
                _dtnc[i].augment(l2nm + "tev", -1);
                _l2nc[i].augment(l2nm + "fiv", -1);
                _l2nc[i].augment(l2nm + "fev",  1 - loss); // import vars reduce link power
                _l2nc[i].augment(l2nm + "tiv", -1);
                _l2nc[i].augment(l2nm + "tev",  1 - loss); // import vars reduce link power

                _dfizc[i] = lpm.ConsDef(l2nm + "fizc", false, 0, new object[2] {l2nm + "fiv", 1});
                _dfezc[i] = lpm.ConsDef(l2nm + "fezc", false, 0, new object[2] {l2nm + "fev", 1});
                _dtizc[i] = lpm.ConsDef(l2nm + "tizc", false, 0, new object[2] {l2nm + "tiv", 1});
                _dtezc[i] = lpm.ConsDef(l2nm + "tezc", false, 0, new object[2] {l2nm + "tev", 1});
                _dfemc[i] = lpm.ConsDef(l2nm + "femc", false, capf, new object[2] {l2nm + "fev", -1});
                _dtemc[i] = lpm.ConsDef(l2nm + "temc", false, capt, new object[2] {l2nm + "tev", -1});

                //
                i++;
            }


        }

        //
        // Update the resulting model capacities & costs
        //
        public void Update(LP mlp, ElsiPeriod? period=null) {
            int i, j;
            double capf, capt, loss;
            double itf, itt;

            #if DEBUG
                PrintFile.PrintVars("Link2");
            #endif

            i=0;
            foreach( var l in _data.Links.Rows) {
                capf = l.Capacity;
                capt = l.RevCap;
                loss =  l.Loss;

                itf = l.ITF;
                itt =l.ITT;

                mlp.SetCvec(_dfivar[i].Id,itf);  // Set tariffs
                mlp.SetCvec(_dtivar[i].Id,itt);
                mlp.SetBvec(_dfemc[i].Id,capf);  // Set capacities
                mlp.SetBvec(_dtemc[i].Id, capt); 
                mlp.SetEvec(_dfnc[i].Id, 0);     // No demand adders
                mlp.SetEvec(_dtnc[i].Id, 0);

                #if DEBUG
                    PrintFile.PrintVars("i", i, "dfivar.Id", _dfivar[i].Id, "itf", itf, "dtivar.Id", _dtivar[i].Id, "itt", itt);
                    PrintFile.PrintVars("i", i, "dfemc.Id", _dfemc[i].Id, "capf", capf, "dtemc.Id", _dtemc[i].Id, "capt", capt);
                    PrintFile.PrintVars("i", i, "dfnc.Id", _dfnc[i].Id, "dtnc.Id", _dtnc[i].Id);
                #endif    

                // Skip link contraints that are not market resolved
                if ( !l.Market ) {
                    mlp.SetSkip(_dfemc[i].Id, true);
                    mlp.SetSkip(_dtemc[i].Id, true);
                    #if DEBUG
                        PrintFile.PrintVars("i", i, "Skip", _dfemc[i].Id);
                        PrintFile.PrintVars("i", i, "Skip", _dtemc[i].Id);
                    #endif
                }
                i++;
            }
        }

        // Set run type
        // Auxdata (0 .. lupb, 0 .. 1) gives baseflows at from and to ends
        public void SetPhase(LP mlp, int phaseId, object[,] auxdata) {
            int i;
            if ( phaseId == 0 ) {  // Market phase
                Update(mlp); // Reset links
            } else {
                i=0;
                foreach(var l in _data.Links.Rows) {
                    if ( l.Market ) {
                        auxdata[i,0] = mlp.Slack(_dfizc[i].Id) - mlp.Slack(_dfezc[i].Id);
                        auxdata[i,1] = mlp.Slack(_dtizc[i].Id) - mlp.Slack(_dtezc[i].Id);
                        mlp.SetCvec(_dfivar[i].Id, l.ITF + l.BTF); // Set tariffs and premia
                        mlp.SetCvec(_dtivar[i].Id, l.ITT + l.BTT); // 
                        mlp.SetBvec(_dfemc[i].Id, l.Capacity + (double) auxdata[i,0]); //  set capacities less export
                        mlp.SetBvec(_dtemc[i].Id, l.RevCap   + (double) auxdata[i,1]);
                        mlp.SetEvec(_dfnc[i].Id, mlp.GetEvec(_dfnc[i].Id) + (double) auxdata[i,0]); // adjust demands
                        mlp.SetEvec(_dtnc[i].Id, mlp.GetEvec(_dtnc[i].Id) + (double) auxdata[i,1]);
                    } else {
                        mlp.SetSkip(_dfemc[i].Id, false);
                        mlp.SetSkip(_dtemc[i].Id, false);
                    }
                    i++;
                }
            }
        }

        // Initialise the resulting model
        public void Initialise(LP mlp, double[,] smp) {
            int i, fz, tz;

            for(i=0; i<_l2nc.Length; i++) {
                fz = _zfr[i];
                tz = _ztr[i];
                if ( smp[fz,1] <= smp[tz,1]) {
                    mlp.EnterBasis(_dfivar[i].Id, _dfizc[i].Id);
                    mlp.EnterBasis(_dtevar[i].Id, _dtezc[i].Id);
                    if ( mlp.GetSkip(_dfemc[i].Id)) {
                        mlp.EnterBasis(_dfevar[i].Id, _dfezc[i].Id);
                    } else {
                        mlp.EnterBasis(_dfevar[i].Id, _dfemc[i].Id);
                    }
                    mlp.EnterBasis(_dtivar[i].Id, _l2nc[i].Id);
                } else {
                    mlp.EnterBasis(_dfivar[i].Id, _l2nc[i].Id);
                    if ( mlp.GetSkip(_dtemc[i].Id)) {
                        mlp.EnterBasis(_dtevar[i].Id, _dtezc[i].Id);
                    } else {
                        mlp.EnterBasis(_dtevar[i].Id, _dtemc[i].Id);
                    }
                    mlp.EnterBasis(_dfevar[i].Id, _dfezc[i].Id);
                    mlp.EnterBasis(_dtivar[i].Id, _dtizc[i].Id);
                }
            }
        }

        // Get the flows amd prices for market managed links ready for subsequent balancing phase
        public void GetFlows(LP mlp, object[,] margp, out object[,] auxdata) {
            int i, per;
            auxdata = new object[_l2nc.Length,5];
            per = mlp.Id; // Identify period
            i=0;
            foreach( var l in _data.Links.Rows) {
                if ( l.Market) {
                    auxdata[i,0] = mlp.Slack(_dfizc[i].Id) - mlp.Slack(_dfezc[i].Id);
                    auxdata[i,1] = mlp.Slack(_dtizc[i].Id) - mlp.Slack(_dtezc[i].Id);
                    auxdata[i,2] = margp[_zfr[i] + 1, per +1];
                    auxdata[i,3] = margp[_ztr[i] + 1,per + 1];
                    auxdata[i,4] = mlp.GetCvec(_dfivar[i].Id) * mlp.Slack(_dfizc[i].Id) + mlp.GetCvec(_dtivar[i].Id)*mlp.Slack(_dtizc[i].Id);
                }
                i++;
            }
        }

        // Extract output corresponding to original data table
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray) {
            int i, per;
            double fp, loss, tp;

            oparray = new object[_l2nc.Length*2 + 3,2];

            switch (dtype) {
                case ModelConsts.d_name:
                    i=0;
                    foreach( var l in _data.Links.Rows) {
                        oparray[i*2 + 1,1] = l.FromZone.ToString() + ":" + l.Name;
                        oparray[i*2 + 2,1] = l.ToZone.ToString() + ":" + l.Name;
                        i++;
                    }
                    break;
                case ModelConsts.d_sched:
                    for(i=0;i<_l2nc.Length;i++) {
                        oparray[i*2 + 1,1] = mlp.Slack(_dfizc[i].Id) - mlp.Slack(_dfezc[i].Id) + auxdata[i,0];
                        oparray[i*2 + 2,1] = mlp.Slack(_dtizc[i].Id) - mlp.Slack(_dtezc[i].Id) + auxdata[i,1];
                    }
                    break;
                case ModelConsts.d_price:
                case ModelConsts.d_offers:
                case ModelConsts.d_bids: // auxdata are zone marginal prices
                    per = mlp.Id;
                    i=0;
                    foreach( var l in _data.Links.Rows) {
                        loss = 1 + l.Loss;
                        fp = auxdata[_zfr[i] + 1, per + 1];
                        tp = auxdata[_ztr[i] + 1, per + 1];

                        if ( fp > tp ) { // from-end importing
                            // from-end production cost = to-end (exporting price) plus loss plus import tariff
                            oparray[i*2 + 1,1] = tp * loss + mlp.GetCvec(_dfivar[i].Id);
                            // to-end production cost = from-end (importing price) less import tariff less loss
                            oparray[i*2 + 2,1] = (fp - mlp.GetCvec(_dfivar[i].Id)) / loss;
                        } else { // to-end importing
                            // from-end production cost = to-end importing price less remote import tariff
                            oparray[i*2 + 1,1] = (tp - mlp.GetCvec(_dtivar[i].Id)) / loss;
                            // to-end production cost = from-end exporting price plus import tariff
                            oparray[i*2 + 2,1] = fp * loss + mlp.GetCvec(_dtivar[i].Id);
                        }
                        i++;
                    }
                    break;
                case ModelConsts.d_avail:
                    i=0;
                    foreach( var l in _data.Links.Rows) {
                        oparray[i*2+1,1] = l.Capacity;
                        oparray[i*2+2,1] = l.RevCap;
                        i++;
                    }
                    break;
                case ModelConsts.d_cost:
                    i=0;
                    foreach(var l in _data.Links.Rows) {
                        oparray[i*2 + 1,1] = mlp.GetCvec(_dfivar[i].Id);
                        oparray[i*2 + 2,1] = mlp.GetCvec(_dtivar[i].Id);
                        i++;
                    }
                    break;

                case ModelConsts.d_balcost:
                    i=0;
                    foreach(var l in _data.Links.Rows) {
                        oparray[i*2 + 1,1] = l.ITF + l.BTF;
                        oparray[i*2 + 2,1] = l.ITT + l.BTT;
                        i++;
                    }
                    break;
            }
        }

        // Decide how many market price areas
        public void MarketAreas(out int[] zmembers) {
            int i;
            bool chng;
            zmembers = new int[_data.ZDem.Count];
            for(i=0;i<zmembers.Length;i++) {
                zmembers[i] = i;   // Start by assuming all zones are their own price area
            }
            
            do {
                chng = false;
                i=0;
                foreach( var l in _data.Links.Rows) {
                    if ( !l.Market) {
                        if ( zmembers[_zfr[i]] < zmembers[_ztr[i]]) {
                            zmembers[_ztr[i]] = zmembers[_zfr[i]];
                            chng = true;
                        } else if ( zmembers[_zfr[i]] > zmembers[_ztr[i]]) {
                            zmembers[_zfr[i]] = zmembers[_ztr[i]];
                            chng = true;
                        }
                    }
                    i++;
                }
            } while (chng);
        }
    }
}