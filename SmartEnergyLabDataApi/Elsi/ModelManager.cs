/* VBA code
' Build and manage the day model
'
'
Option Explicit
Option Base 0


Public paramtab As New DTable
Public pertab As New DTable
Public ztab As New DTable
Public gtab As New DTable
Public stab As New DTable
Public ltab As New DTable
Public btab As New DTable

Public mparts As Collection ' master problem model parts
Public sparts As Collection ' sub-problem model parts
Public tsmodel As LPModel   ' timestep model
Public perlp() As LP        ' resulting LPs one per period
Public daymodel As LPModel  ' day model
Public daylp As LP          ' resulting LP
Public mvlist() As Variant  ' master variable list
Public linkflows() As Variant   ' Saved link flows for each period
Public dayn As Long         ' modelled day
Public markets As Collection

Public parts As Collection
Public bprt As Benders
Public zprt As Zones
Public sprt As Stores
Public lprt As Link2
Public gprt As Gens

Public berr As Double       ' Benders error
Public iters() As Long      ' sub problem calls
Public tsobj() As Double    ' Sub problem objectives

Public atrng As Range        ' title range for availability output
Public mtrng As Range        ' title range for market schedule
Public btrng As Range        ' title range for balance schedule
Public bmtrng As Range       ' title range for balance mechanism outputs



' Run current day

Public Sub RunDay()
    Dim i As Long
    
'    If tsmodel Is Nothing Then
        BuildModel
'    End If
        
'    For i = 1 To 5
    UpdateModel
    MarketDay
    BalanceDay
'    Next i
End Sub

' Run year

Public Sub RunDays()
    Dim oday As Long, i As Long
    Dim r As Long, pv() As Variant
    Dim sd As Long, ed As Long
    Dim c_param As Long, c_val As Long
    
    oday = Range("DayN").Value
  
    If tsmodel Is Nothing Then
        BuildModel
    End If
    
    c_param = paramtab.FindCol("Parameter")
    c_val = paramtab.FindCol("Value")
    paramtab.GetData pv
    r = FindParam("Start_Day", pv, c_param)
    sd = pv(r, c_val)
    r = FindParam("End_Day", pv, c_param)
    ed = pv(r, c_val)
    
    For i = sd To ed
        Range("DayN").Value = i
        Application.StatusBar = "Calculating day " & CStr(i)
        Application.Worksheets("Day").Calculate
        UpdateModel
        MarketDay
        BalanceDay
        DoEvents
    Next i
    Application.StatusBar = ""
    Range("DayN").Value = oday
End Sub

Private Function FindParam(str As String, pv() As Variant, cp As Long) As Long
    Dim i As Long
    
    For i = LBound(pv, 1) To UBound(pv, 1)
        If pv(i, cp) = str Then
            FindParam = i
            Exit Function
        End If
    Next i
    FindParam = 0
        
End Function


' Build the model (constant for year)
' and make output titles

Public Sub BuildModel()
    Dim i As Long
    Dim pupb As Long
    Dim orng As Range, r As Long, c As Long
    Dim mpart As IMaster
    
    ' Link to model input tables

    paramtab.Init "ParamTable", Array("Parameter", "Value")
    pertab.Init "PerTable", Array("Period", "Hours")
    ztab.Init "ZDemTable", Array("Zone", "Profile", "Annual_Pk", "Pk", "Pl", "So", "Pu", "Tr", "Market")
    gtab.Init "GenTable", Array("GenName", "Zone", "Capacity", "Wavail", "Oavail", "AvailTable", "tCO2", "£/tCO2", "Price", "Bid", "Offer", "Flex1", "Flex2")
    stab.Init "StoreTable", Array("StoreName", "Zone", "Capacity", "MaxPump", "WAvail", "OAvail", "CycleEff", "Endurance", "Bid", "Offer", "Flex1", "Flex2")
    ltab.Init "LinkTable", Array("LinkName", "From", "To", "Capacity", "RevCap", "Loss", "Market", "ITF", "ITT", "BTF", "BTT")
'    btab.Init "BounTable"
    
    pupb = pertab.RowCount - 1
    ReDim perlp(pupb) As LP
    
    ' Create the master and sub problem models
    
    Set mparts = New Collection
    Set daymodel = NewLPModel()
    
    Set sparts = New Collection
    Set tsmodel = NewLPModel()
    
    ' create the sub problem model first starting with zones part
    
    Set zprt = New Zones
    zprt.IPart_partname = "ZPart"
    sparts.Add zprt, zprt.IPart_partname
    zprt.Ipart_Build tsmodel, ztab
    
    ' Create the Links part
    Set lprt = New Link2
    lprt.IPart_partname = "L2Part"
    sparts.Add lprt, lprt.IPart_partname
    lprt.Ipart_Build tsmodel, ltab
    
    ' Create the gens part
    Set gprt = New Gens
    gprt.IPart_partname = "GPart"
    sparts.Add gprt, gprt.IPart_partname
    gprt.Ipart_Build tsmodel, gtab
    
    ' Create the Benders part
    Set bprt = New Benders
    bprt.IPart_partname = "BPart"
    mparts.Add bprt, bprt.IPart_partname
    bprt.Ipart_Build daymodel, Nothing
    
    ' Create the stores part
    Set sprt = New Stores
    sprt.IPart_partname = "SPart"
    mparts.Add sprt, sprt.IPart_partname
    sprt.Ipart_Build daymodel, stab
        
    Set daylp = daymodel.MakeLP()
    daylp.Id = -1
    
    For i = 0 To pupb
        Set perlp(i) = tsmodel.MakeLP()
        perlp(i).Id = i
    Next i
        
    ' Make Result titles
    
    
    Set orng = Range("ResBase")
    
    Set atrng = AvailTitles(orng.Offset(r, c))
    r = r + atrng.Rows.Count + 3
    
    orng.Offset(r - 2, 0).Value = "Market phase:"
    
    Set mtrng = ScheduleTitles(orng.Offset(r, c), False)
    r = r + mtrng.Rows.Count + 3
    
    orng.Offset(r - 2, 0).Value = "Balance phase:"
    
    Set btrng = ScheduleTitles(orng.Offset(r, c), True)
    r = r + btrng.Rows.Count + 3
    
    orng.Offset(r - 2, 0).Value = "Balance Mechanisms:"
    
    Set bmtrng = BMTitles(orng.Offset(r, c))
    
End Sub

' Update model for a specific day
' and output availability data

Public Sub UpdateModel()
    Dim i As Long, npers As Long
    Dim part As IPart
    Dim arng As Range
    
    dayn = Range("Dayn").Value
    npers = pertab.RowCount
    
    
    ' master problem update
    For Each part In mparts
        part.Update daylp
    Next part
    
    ' sub problem updates
    For i = 0 To npers - 1
        For Each part In sparts
            part.Update perlp(i), pertab.GetCell(i + 1, "Period")
        Next part
    Next i
    
    Set arng = atrng.Offset(0, atrng.Columns.Count + (dayn - 1) * npers)
    Set arng = OutAvail(arng.Resize(1, 1), dayn)
End Sub


' Calculate a cold start

Public Sub ColdStart()
    Dim i As Long, j As Long, grp As Long
    Dim pupb As Long, zupb As Long, pos As Long
    Dim part As IPart
    Dim tdem As Double, znm As String
    Dim mnm As String, sl As Double, sz As Double, pr As Double
    Dim zonemo() As MO, grpmo() As MO
    Dim smp() As Variant
    Dim zgrp() As Long, grpdem() As Double, zndem() As Double
    Dim c_zone As Long, c_per As Long
    
    pupb = pertab.RowCount - 1
    c_per = pertab.FindCol("Period")
    zupb = ztab.RowCount - 1
    c_zone = ztab.FindCol("Zone")
    
    lprt.MarketAreas zgrp
    
    ' calc smps and initialise subproblem parts
    
    For i = 0 To pupb
    
'        perlp(i).InitCOrder             ' Set natural constraint order
        
        ReDim zonemo(zupb) As MO
        ReDim grpmo(zupb) As MO
        ReDim grpdem(zupb) As Double
        ReDim zndem(zupb) As Double
        ReDim smp(zupb, 1) As Variant
    
        For j = 0 To zupb
            grp = zgrp(j)
            znm = ztab.GetCell(j + 1, "Zone")
            Set zonemo(j) = New MO
            If grpmo(grp) Is Nothing Then
                Set grpmo(j) = New MO
            Else
                Set grpmo(j) = grpmo(grp)
            End If
            zndem(j) = zprt.ZoneDemand(perlp(i), znm)   ' get zone demand
            grpdem(grp) = grpdem(grp) + zndem(j)
        Next j
        gprt.SystemMO perlp(i), zonemo      ' Get zone merit orders for this period
        gprt.SystemMO perlp(i), grpmo       ' Get group merit orders
        
        For j = 0 To zupb
            grp = zgrp(j)
            znm = ztab.GetCell(j + 1, "Zone")
            pos = grpmo(j).Vschedule(grpdem(grp), sl)     ' calc marginal gen
            grpmo(j).PosDetails pos, mnm, pr, sz
            smp(j, 0) = pr
            
            With zonemo(j)
                pos = .Vschedule(zndem(j), sl)
                .PosDetails pos, mnm, pr, sz
                 smp(j, 1) = pr
            End With
            Debug.Print "Period "; pertab.GetCell(i + 1, "Period"); " Zone "; znm; " Marginal "; mnm; " SMP"; smp(j, 0); " LMP"; smp(j, 1)
        Next j
        
        gprt.Ipart_Initialise perlp(i), smp
        lprt.Ipart_Initialise perlp(i), smp
        
        ' Do zone constraint last
        zprt.Ipart_Initialise perlp(i), smp
    Next i
    
    ' initialise master problem parts
    
    daylp.InitCOrder
    For Each part In mparts
        part.Initialise daylp, smp
    Next part
    
    ' Set master problem variables
    ' daylp is the master problem
    ' perlp(i) are the period sub problems
    bprt.InitVarList daylp, perlp
    
End Sub

Public Sub zmps(oparray() As Variant)
    Dim tarray() As Variant
    
    bprt.Ipart_Outputs daylp, d_price, tarray, oparray
End Sub

Public Sub debugoutput(per As Long)
    Dim znm As String, i As Long
    Dim tarray() As Variant, oparray() As Variant, nmarray() As Variant
    Dim c_zone As Long
    
    c_zone = ztab.FindCol("Zone")
    
    zprt.Ipart_Outputs perlp(per), d_price, tarray, oparray
    For i = 0 To ztab.RowCount - 1
        znm = ztab.Content(i + 1, c_zone)
        Debug.Print "Zone "; znm; " Shadow "; oparray(i + 1, 1)
    Next i
    
    ReDim tarray(ltab.RowCount - 1, 3) As Variant
    lprt.Ipart_Outputs perlp(per), d_name, tarray, nmarray
    lprt.Ipart_Outputs perlp(per), d_sched, tarray, oparray
    For i = 1 To UBound(nmarray, 1)
        Debug.Print nmarray(i, 1); " Flow "; oparray(i, 1)
    Next i
End Sub


' Solves the day using Bender's decomposition


Public Sub SolveDay(schedname As String)
    Dim i As Long, perhours() As Variant
    Dim lcost() As Double, hours() As Double
    Dim pmax As Long, c_hours As Long
    
    pmax = pertab.RowCount - 1
    pertab.GetData perhours
    c_hours = pertab.FindCol("Hours")
    ReDim hours(pmax) As Double
    ReDim lcost(pmax) As Double
    
    For i = 0 To pmax
        hours(i) = perhours(i + 1, c_hours)
        lcost(i) = linkcosts(i)
    Next i

    berr = bprt.Solve(schedname, lcost, hours)
    bprt.SPStatus iters, tsobj
End Sub

' Run Market phase

Public Sub MarketDay()
    Dim msrng As Range
    Dim pupb As Long, lupb As Long
    Dim i As Long
    Dim tarray() As Variant
    
    pupb = pertab.RowCount - 1
    lupb = ltab.RowCount - 1
    ColdStart
        
    ReDim tarray(lupb, 4)           '0=from flow, 1=to flow, 2=from price, 3=to price, 4=link costs
    ReDim linkflows(pupb) As Variant
    For i = 0 To pupb
        linkflows(i) = tarray
    Next i
    
    SolveDay "Mkt"
    Set msrng = mtrng.Offset(0, atrng.Columns.Count + (dayn - 1) * (pupb + 1))
    OutSchedule msrng, dayn, "Mkt"
End Sub

' Run balance phase

Public Sub BalanceDay()
    Dim bsrng As Range, bmrng As Range
    Dim i As Long
    Dim flows() As Variant, margp() As Variant
    Dim pupb As Long, lupb As Long
    Dim part As IPart
    Dim tarray() As Variant, smp() As Variant
    
    pupb = pertab.RowCount - 1
    lupb = ltab.RowCount - 1
    
    zmps margp          ' Get market marginal prices
    
    For i = 0 To UBound(perlp)
'        perlp(i).NormCOrder
        
        lprt.GetFlows perlp(i), margp, flows
        linkflows(i) = flows                    ' save market flows
        
        For Each part In sparts                 ' set up balancing phase with warmstart of sub problems
            part.SetPhase perlp(i), 1, flows
        Next part
    Next i
    
    daylp.InitCOrder
    For Each part In mparts
        part.Initialise daylp, smp              'master problem reset
    Next
    
    SolveDay "Bal"
    Set bsrng = btrng.Offset(0, atrng.Columns.Count + (dayn - 1) * (pupb + 1))
    OutSchedule bsrng, dayn, "Bal"
    
    Set bmrng = bmtrng.Offset(0, atrng.Columns.Count + (dayn - 1) * (pupb + 1))
    BMResults bmrng, dayn
End Sub


' Calculate the total link costs (saved in linkflows)

Private Function linkcosts(per As Long) As Double
    Dim i As Long
    Dim res As Double
    Dim lflows() As Variant
    
    lflows = linkflows(per)
    For i = 0 To UBound(lflows, 1)
        res = res + lflows(i, 4)
    Next i
    linkcosts = res
End Function
*/

using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;
using SmartEnergyLabDataApi.Elsi.LinearProgramming;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SmartEnergyLabDataApi.Elsi
{
    public class ModelManager {

        private Collection<IMaster> _mparts;     // master problem model parts
        private Collection<IPart> _sparts;     // sub-problem model parts
        public LPModel tsmodel;       // timestep model
        public LP[] perlp;            // resulting LPs one per period
        public LPModel daymodel;      // day model
        public LP daylp;              // resulting LP
        public object[] mvlist;       // master variable list
        public object[] linkflows;    // Saved link flows for each period
        
        public Benders bprt;
        public Zones zprt;
        public Stores sprt;
        public Link2 lprt;
        public Gens gprt;

        private double _berr;
        public int[] iters;          // sub problem calls
        public double[] tsobj;       // Sub problem objectives


        private Collection<string> _markets;

        private ElsiData _data;
        private ElsiLog? _log;
        private bool _parallelProcessing;

        
        public ModelManager(ElsiData data, ElsiLog? log=null, bool parallelProcessing = false) {            
            _data = data;
            _log = log;
            _parallelProcessing = parallelProcessing;
        }

        public ElsiData Data {
            get {
                return _data;
            }
        }

        public ElsiLog? Log {
            get {
                return _log;
            }
        }

        public Collection<string> Markets {
            get {
                return _markets;
            }
            set {
                _markets = value;
            }
        }

        public Gens Gens {
            get {
                return gprt;
            }
        }

        public Zones Zones {
            get {
                return zprt;
            }
        }

        public Stores Stores {
            get {
                return sprt;
            }
        }

        public Link2 Links {
            get {
                return lprt;
            }
        }

        public Collection<IMaster> MParts {
            get {
                return _mparts;
            }
        }

        public LP[] PerLp {
            get {
                return perlp;
            }
        }

        public LP DayLp {
            get {
                return daylp;
            }
        }

        public double Berr {
            get {
                return _berr;
            }
            set {
                _berr = value;
            }
        }

        public void NewStatusMessage(string sm) 
        {
            _log?.WriteStr(sm);
            Console.WriteLine(sm);
        }

        public ElsiDayResult RunDay(int day) {
            
            _data.SetDay(day);
            ElsiDayResult result = new ElsiDayResult(this);
            BuildModel();

            UpdateModel();
            result.CreateAvailabilityResults();

            MarketDay();
            result.CreateMarketResults();

            BalanceDay();
            result.CreateBalanceResults();

            result.CreateMismatchResults();

            return result;
        }

        public void RunDays(int startDay, int endDay) {
            int oday, i;
            int sd, ed;
            if ( startDay>endDay) {
                throw new Exception($"Start day must be less than end day");
            }

            NewStatusMessage($"Starting multi-day run");
            for(i=startDay;i<=endDay;i++) {
                _data.SetDay(i);
                NewStatusMessage($"Calculating day {i}");
                RunDay(i);
            }
            NewStatusMessage($"Completed multi-day run");
        }

        public void BuildModel() {
            int i;
            IMaster mpart;

            perlp = new LP[_data.Per.Count];

            _mparts = new Collection<IMaster>();
            daymodel = LPhdr.NewLPModel();

            _sparts = new Collection<IPart>();
            tsmodel = LPhdr.NewLPModel();

            // create the sub problem model first starting with the zones part
            zprt = new Zones();
            zprt.PartName = "ZPart";
            _sparts.Add(zprt,zprt.PartName);
            zprt.Build(this, tsmodel);

            // Create the Links part
            lprt = new Link2();
            lprt.PartName = "L2Part";
            _sparts.Add(lprt,lprt.PartName);
            lprt.Build(this,tsmodel);

            // Create the gens part
            gprt = new Gens();
            gprt.PartName = "GPart";
            _sparts.Add(gprt, gprt.PartName);
            gprt.Build(this,tsmodel);

            // Create the Benders part
            bprt = new Benders();
            bprt.PartName = "BPart";
            _mparts.Add(bprt,bprt.PartName);
            bprt.Build(this, daymodel);

            // Create the stores part
            sprt = new Stores();
            sprt.PartName = "Spart";
            _mparts.Add(sprt, sprt.PartName);
            sprt.Build(this,daymodel);

            daylp = daymodel.MakeLP();
            daylp.Id = -1;

            for(i=0;i<_data.Per.Count;i++) {
                perlp[i] = tsmodel.MakeLP();
                perlp[i].Id = i;
            }            

        }

        // Update model for a specific day
        public void UpdateModel() {
            // Master problem update
            foreach( var part in _mparts.Items) {
                if ( part is IPart ) {
                    ((IPart) part).Update(daylp);
                } 
            }

            // Sub problem updates
            int i=0;
            foreach( var p in _data.Per.Items) {
                foreach( var part in _sparts.Items) {
                    part.Update(perlp[i], p.Period);
                }
                i++;
            }
        }

        // Calculate a cold start
        public void ColdStart() {
            int i, j, grp, pos;
            double tdem, sl=0, sz=0, pr=0;
            ElsiMainZone znm;
            string mnm="";
            MO[] zonemo, grpmo;
            double[] grpdem, zndem;
            int[] zgrp;
            double[,] smp=null;

            lprt.MarketAreas(out zgrp);

            #if DEBUG
                PrintFile.PrintVars("Coldstart");
            #endif

            // Calc smps and initialise subproblem parts
            i=0;
            foreach( var p in _data.Per.Items) {
                zonemo = new MO[_data.ZDem.Count];
                grpmo = new MO[_data.ZDem.Count];
                grpdem = new double[_data.ZDem.Count];
                zndem = new double[_data.ZDem.Count];
                smp = new double[_data.ZDem.Count,2];
                j=0;
                foreach( var z in _data.ZDem.Items) {
                    grp = zgrp[j];
                    znm = z.Zone;
                    zonemo[j] = new MO();
                    if ( grpmo[grp]==null ) {
                        grpmo[j] = new MO();
                    } else {
                        grpmo[j] = grpmo[grp];
                    }
                    zndem[j] = zprt.ZoneDemand(perlp[i], znm); // Get zone demand
                    grpdem[grp] = grpdem[grp] + zndem[j];
                    j++;
                }
                gprt.SystemMO(perlp[i],zonemo); // Get zone merit orders for this period
                gprt.SystemMO(perlp[i],grpmo);  // Get group merit orders

                j=0;
                foreach( var z in _data.ZDem.Items) {
                    grp = zgrp[j];
                    znm = z.Zone;
                    //
                    pos = grpmo[j].Vschedule(grpdem[grp],ref sl);  // Calc marginal gen
                    grpmo[j].PosDetails(pos, ref mnm, ref pr, ref sz);
                    smp[j,0] = pr;
                    //
                    pos = zonemo[j].Vschedule(zndem[j], ref sl);
                    zonemo[j].PosDetails(pos, ref mnm, ref pr, ref sz);
                    smp[j,1] = pr;
                    //
                    Debug.Print($"Period {p.Period}, zone {znm} Marginal {mnm} SMP {smp[j,0]} LMP {smp[j,1]}");
                    j++;
                }

                gprt.Initialise(perlp[i], smp);
                lprt.Initialise(perlp[i], smp);

                // Do zone contraint last
                zprt.Initialise(perlp[i], smp);

                #if DEBUG
                for(j=0; j<smp.GetLength(0);j++) {
                    //PrintFile.PrintVars("i", i, "j", j, "smp0", smp[j, 0], "smp1", smp[j, 1]);
                }
                #endif

                i++;
            }

            // Initialise master problem parts
            daylp.InitCOrder();
            foreach( var part in _mparts.Items) {
                if ( part is IPart) {
                    ((IPart)part).Initialise(daylp, smp);
                }
            }

            // Set master problem variables
            // daylp is the master problem
            // perlp[i] are the period sub problems
            bprt.InitVarList(daylp, perlp);

        }

        public void zmps(out object[,] oparray) {
            bprt.Outputs(daylp,ModelConsts.d_price, null, out oparray);
        }

        public void debugoutput(int per) {
            ElsiMainZone znm;
            int i;
            double[,] tarray=null;
            object[,] oparray, nmarray;

            zprt.Outputs(perlp[per], ModelConsts.d_price, tarray, out oparray);
            i=0;
            foreach( var z in _data.ZDem.Items) {
                znm = z.Zone;
                Console.WriteLine($"Zone {znm} Shadow {oparray[i+1,1]}");
                i++;
            }

            tarray = new double[_data.Links.Count,4];
            lprt.Outputs(perlp[per], ModelConsts.d_name, tarray, out nmarray);
            lprt.Outputs(perlp[per], ModelConsts.d_sched, tarray, out oparray);
            for( i=1;i<nmarray.Length;i++) {
                Console.WriteLine($"{nmarray[i,1]} Flow {oparray[i,1]}");
            }
        }

        // Solves the day using Bender's decomposition
        public void SolveDay(string schedname) {
            int i;
            double[] lcost, hours;
            hours = new double[_data.Per.Count];
            lcost = new double[_data.Per.Count];

            #if DEBUG
                PrintFile.PrintVars("SolveDay");
            #endif 

            i=0;
            foreach( var p in _data.Per.Items) {
                hours[i] = p.Hours;
                lcost[i] = linkcosts(i);
                #if DEBUG
                    PrintFile.PrintVars("i",i,"hours",hours[i],"linkcost",lcost[i]);
                #endif
                i++;
            }
            _berr = bprt.Solve(schedname, lcost, hours);
            bprt.SPStatus(out iters, out tsobj);
        }

        // Run market phase
        public void MarketDay() {
            int i;
            double[,] tarray;


            ColdStart();
            tarray = new double[_data.Links.Count,5]; // 0=from flow, 1=to flow, 2=from price, 3=to price, 4-link costs
            linkflows = new object[_data.Per.Items.Count];
            for(i=0;i<linkflows.Length;i++) {
                linkflows[i] = Utilities.CopyArray(tarray);
            }

            SolveDay("Mkt");
            
        }

        // Run balance day
        public void BalanceDay() {
            int i;
            object[,] flows, margp;
            double[,] smp=null;

            zmps(out margp);                // Get market marginal prices

            for(i=0;i<perlp.Length;i++) {

                lprt.GetFlows(perlp[i], margp, out flows); 
                linkflows[i] = flows;   // save market flows

                foreach( var part in _sparts.Items) {        // setup balancing phase with warmstart of sub problems
                    part.SetPhase(perlp[i], 1, flows);
                }
            }

            daylp.InitCOrder();
            foreach( var part in _mparts.Items) {
                if ( part is IPart) {
                    ((IPart)part).Initialise(daylp, smp);     // mast problem reset
                }                
            }

            SolveDay("Bal");
        }

        // Calculate the total link costs (saved in linkflows)
        private double linkcosts(int per) {
            int i;
            double res=0;
            if ( linkflows[per] is double[,]) {
                var lflows = (double[,])linkflows[per];
                for( i=0;i<lflows.GetLength(0);i++) {
                    res = res + (double) lflows[i,4];
                }
            }
            return res;
        }

    }

    public class DayRunner 
    {
        private Task[] _tasks;
        private object _dayLock = new object();
        private int _startDay {get; set;}
        private int _endDay {get; set;}
        private int _nextDay;
        private ElsiScenario _scenario;
        private int _datasetId;
        private ElsiLog? _log;
        public delegate void ElsiProgressHandler(DayRunner sender, ElsiProgressEventArgs e);
        public event ElsiProgressHandler ElsiProgress;
        private object _numCompleteLock = new object();
        private int _numComplete;


        public DayRunner(int sd, int ed, ElsiScenario scenario, int datasetId, ElsiLog? log=null) {
            if ( sd<1 || sd > 365) {
                throw new Exception("Start day out of range 1-365");
            }
            if ( ed<1 || ed > 365) {
                throw new Exception("End day out of range 1-365");
            }
            if ( sd>ed) {
                throw new Exception("Start day must be <= end day");
            } 
            _datasetId = datasetId;
            _startDay = sd;
            _endDay = ed;            
            _scenario = scenario;
            _log = log;

            _tasks = new Task[Environment.ProcessorCount];            
            for(int i=0;i<_tasks.Length;i++) {
                _tasks[i] = new Task(processDays);
            }
            _nextDay = _startDay;
            _numComplete = 0;
        }

        public void Run() {

            foreach( var task in _tasks) {
                task.Start();
            }
        }

        private void processDays() {
            int day = getNextDay();
            while(day!=0) {
                try {
                    //
                    ElsiDayResult result;
                    using ( var da = new DataAccess() ) {
                        var datasetInfo = new DatasetInfo(da, _datasetId);
                        var data = new ElsiData(da, datasetInfo, _scenario);
                        var mm = new ModelManager(data,_log); 
                        result = mm.RunDay(day);
                    }
                    // Need to open a new dataaccess otherwise will save the user edits back into the main data tables
                    using( var da = new DataAccess() ) {
                        saveResults(da, result);
                    }
                } catch( Exception e) {
                    Logger.Instance.LogErrorEvent($"Problem calculating day [{day}]");
                    Logger.Instance.LogException(e);
                } finally {
                    ElsiProgress?.Invoke(this, getProgressEventArgs());
                }
                day = getNextDay();
            }
        }

        private ElsiProgressEventArgs getProgressEventArgs() {
            lock( _numCompleteLock) {
                _numComplete++;
                return new ElsiProgressEventArgs(_numComplete,_endDay-_startDay+1);
            }
        }

        private void saveResults(DataAccess da, ElsiDayResult results) {
            var dataset = da.Datasets.GetDataset(_datasetId);
            if ( dataset!=null ) {

                string json = JsonSerializer.Serialize(results,new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                // Look for an existing ElsiResult
                var er = da.Elsi.GetResult(_datasetId,results.Day,_scenario);
                if ( er==null ) {
                    er = new ElsiResult();            
                    er.Dataset = dataset;
                    er.Day = results.Day;
                    er.Scenario = _scenario;
                    da.Elsi.Add(er);
                }
                er.Data = Encoding.UTF8.GetBytes(json);
                //
                da.CommitChanges();
            }            
        }

        private int getNextDay() {
            lock ( _dayLock ) {
                int day = _nextDay;
                if ( _nextDay!=0) {
                    _nextDay++;
                    if ( _nextDay>_endDay) {
                        _nextDay = 0; // this means we've finished
                    }
                }
                return day;
            }
        }

        public class ElsiProgressEventArgs : EventArgs {
            public ElsiProgressEventArgs(int numComplete, int numToDo) : base() {
                NumComplete = numComplete;
                NumToDo = numToDo;
                PercentComplete = (NumComplete*100)  / NumToDo;
            }
            public int NumComplete {get; private set;}
            public int NumToDo {get; private set;}
            public int PercentComplete {get; private set;}
        }

    }
}