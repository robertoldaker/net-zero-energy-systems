/*
' Timestep model part describing zone demand constraints
'
' Lewis Dale 7 Jan 2019


Option Explicit
Option Base 0

Implements IPart

Private pname As String
Private dcdef() As LPConsDef
Private ztab As DTable
Private c_zone As Long
Private c_dem As Long
Private c_mkt As Long
Private zindex As DIndex


Public Property Get IPart_partname() As String
    IPart_partname = pname
End Property

Public Property Let IPart_partname(ByVal rhs As String)
    pname = rhs
End Property
    
' Build the model part into a LPModel
' csel specifies the period

Public Sub Ipart_Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)
    Dim i As Long, nm As String, dem As Double, per As String
    Dim data() As Variant, mnm As String
    
    If IsMissing(csel) Then
        per = pertab.GetCell(1, "Period")
    Else
        per = csel
    End If
    
    Set markets = New Collection
    Set zindex = New DIndex
    
    Set ztab = dtab
    ztab.GetData data
    c_zone = ztab.FindCol("Zone")
    c_dem = ztab.FindCol(per)
    c_mkt = ztab.FindCol("Market")
    
    ReDim dcdef(1 To ztab.RowCount) As LPConsDef
    
    For i = 1 To ztab.RowCount
        nm = data(i, c_zone)
        
        dem = data(i, c_dem)
        Set dcdef(i) = lpm.ConsDef(nm, True, -dem, Array())
        mnm = data(i, c_mkt)
        
        If mnm <> "" And Not Exists(markets, mnm) Then
            markets.Add mnm, mnm
        End If
    Next i
    zindex.MkIndex data, c_zone
End Sub

Public Function Market(zname As String) As String
    Dim r As Long, data() As Variant
    
    r = ZoneId(zname)
    Market = ztab.GetCell(r, "Market")
End Function

' Update model parameters in the resulting LP
' Data(1 to n, 1 to m) contains name, demand

Public Sub Ipart_Update(mlp As LP, Optional csel As Variant)
    Dim i As Long, nm As String, dem As Double, per As String
    Dim data() As Variant
    Dim c_dem As Long
    
    per = csel
    c_dem = ztab.FindCol(per)
    
    ztab.GetData data
    
    For i = 1 To ztab.RowCount
        dem = data(i, c_dem)
        mlp.bvec(dcdef(i).Id) = -dem
    Next i
End Sub

' Set the model ready for a phase of the solution

Public Sub Ipart_SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)

End Sub

' Initialise the LP based on a system marginal price

Public Sub Ipart_Initialise(mlp As LP, smp() As Variant)
    Dim cmat As SparseMatrix
    Dim i As Long, c As Long, k As Long, v As Long, mv As Long
    Dim s As Double, cst As Double, mc As Double
    
    Set cmat = mlp.TConsMat
    For i = 1 To ztab.RowCount
        mc = -1#
        c = dcdef(i).Id
        k = mlp.TConsMat.FirstKey(c)
        While k <> -1
            cmat.Contents k, v, s
            cst = mlp.cvec(v)
            If s > 0# And cst <= smp(i - 1, 0) And cst > mc Then
                mc = cst
                mv = v
            End If
            k = cmat.NextKey(k, c)
        Wend
        If mc >= 0# Then
            mlp.EnterBasis mv, c
        Else
            Debug.Print "No marginal variable for demand constraint of zone "; ztab.GetCell(i, "Zone")
        End If
    Next i
End Sub

' Provide outputs

Public Sub Ipart_Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)
    Dim i As Long, n As Long
    Dim nmarray() As Variant, tarray() As Variant
    Dim znm As String, zid As Long
    
    n = ztab.RowCount
    
    ReDim oparray(1 To n, 1 To 1) As Variant
    
    Select Case dtype
        Case d_name
            For i = 1 To n
                oparray(i, 1) = ztab.GetCell(i, "Zone")
            Next i
        
        Case d_price
            For i = 1 To n
                oparray(i, 1) = mlp.Shadow(dcdef(i).Id)
            Next i
            
        Case d_avail
            For i = 1 To n
                oparray(i, 1) = mlp.bvec(dcdef(i).Id)
            Next i
            
        Case d_emissions
            gprt.Ipart_Outputs mlp, d_zone, auxdata, nmarray
            gprt.Ipart_Outputs mlp, d_emissions, auxdata, tarray
            For i = 1 To UBound(tarray, 1)
                znm = nmarray(i, 1)
                zid = ZoneId(znm)
                oparray(zid, 1) = oparray(zid, 1) + tarray(i, 1)
            Next i
            
    End Select
    
End Sub


' return zone demand constraintn def
' return nothing if zone not found

Public Function DemConsDef(nm As String) As LPConsDef
    Dim i As Long
  
    i = ZoneId(nm)
  
    If i = -1 Then
        Set DemConsDef = Nothing
    Else
        Set DemConsDef = dcdef(i)
    End If

End Function

Public Function ZoneId(name As String) As Long
    ZoneId = zindex.Lookup(name)
End Function

Public Function ZoneDemand(mlp As LP, znm As String) As Double
    Dim i As Long
    
    i = ZoneId(znm)
    ZoneDemand = -mlp.bvec(dcdef(i).Id)
End Function

Public Sub ZoneMPS(mlp As LP, mps() As Double)
    Dim i As Long
    Dim n As Long
    
    n = UBound(dcdef)
    
    ReDim mps(n - 1) As Double
    
    For i = 0 To n - 1
        mps(i) = mlp.Shadow(dcdef(i + 1).Id)
    Next i
End Sub

Private Function Exists(coll As Collection, key As String) As Boolean
    Dim res As Boolean
    
    On Error GoTo eh
    
    coll.Item key
    Exists = True
    Exit Function

eh:
    Exists = False
End Function


*/
using SmartEnergyLabDataApi.Common;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public class Zones : IPart {
        private ElsiData _data;
        private string _pname;
        private LPConsDef[] _dcdef;        
        private ModelManager _modelManager;
        public Zones() {

        }
        
        public string PartName  {
            get {
                return _pname;
            }
            set {
                _pname = value;
            }
        }

        // Build the model part into a LPModel
        // csel specifies the period

        public void Build(ModelManager modelManager, LPModel lpm, ElsiPeriod per=ElsiPeriod.Pk) {
            string nm, mnm;
            double dem;

            _data = modelManager.Data;
            _modelManager = modelManager;

            modelManager.Markets = new Collection<string>();

            _dcdef = new LPConsDef[_data.ZDem.Count+1];

            var zdem = _data.ZDem;
            ElsiData.ZDemData.Row row;

            for( int i=1; i<=zdem.Count;i++) {
                row = zdem.GetRow(i);
                nm = row.Zone.ToString();
                dem = row.GetDemand(per);
                _dcdef[i] = lpm.ConsDef(nm, true, -dem, new object[0]);
                mnm = row.Market;
                if ( mnm!=null && !modelManager.Markets.ContainsKey(mnm)) {
                    modelManager.Markets.Add( mnm,mnm);
                }
            }
        }

        public string Market(ElsiMainZone mainZone) {
            var row = _data.ZDem.GetRow(mainZone);
            return row.Market;
        }

        // Update model parameters in the resulting LP
        public void Update(LP mlp, ElsiPeriod? period=null)
        {
            #if DEBUG
                PrintFile.PrintVars("Zones");
            #endif

            double dem;
            var zdem = _data.ZDem;
            ElsiData.ZDemData.Row row;
            for( int i=1; i<=zdem.Count;i++) {
                row = zdem.GetRow(i);
                dem = row.GetDemand((ElsiPeriod) period);
                mlp.SetBvec(_dcdef[i].Id, -dem);
                #if DEBUG
                    PrintFile.PrintVars("i", i, "dcdef.Id", _dcdef[i].Id, "-dem", -dem);
                #endif
            }
        }

        public void SetPhase(LP mlp, int phaseid, object[,] auxdata) {

        }

        // Initialise the LP based on a system marginal price
        public void Initialise(LP mlp, double[,] smp) {
            SparseMatrix cmat;
            int i, c, k, v=0, mv=0;
            double s=0, cst, mc;

            cmat = mlp.TConsMat;
            for(i=1; i<=_data.ZDem.Count;i++) {
                mc = -1;
                c = _dcdef[i].Id;
                k = mlp.TConsMat.FirstKey(c);
                while( k!=-1) {
                    cmat.Contents(k,ref v,ref s);
                    cst = mlp.GetCvec(v);
                    if ( s > 0 && cst <= smp[i-1,0] && cst > mc) {
                        mc = cst;
                        mv = v;
                    }
                    k = cmat.NextKey(k,c);
                }
                if ( mc>=0) {
                    mlp.EnterBasis(mv,c);
                } else {
                    var zone = _data.ZDem.GetRow(i).Zone.ToString();
                    Console.WriteLine($"No marginal variable for demand constraint of zone {zone}");
                }
            }
        }

        // Provide outputs
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray) {
            int i, n, zid;
            ElsiMainZone znm;
            n = _data.ZDem.Count;
            object[,] nmarray, tarray;
            oparray = new object[n+1,2];

            switch(dtype) {
                case ModelConsts.d_name:
                    i=1;
                    foreach ( var z in _data.ZDem.Items ) {
                        oparray[i,1] = z.Zone;
                        i++;
                    }
                    break;
                case ModelConsts.d_price:
                    for (i=1; i<=n;i++) {
                        oparray[i,1] = mlp.Shadow(_dcdef[i].Id);
                    }
                    break;
                case ModelConsts.d_avail:
                    for (i=1;i<=n;i++) {
                        oparray[i,1] = mlp.GetBvec(_dcdef[i].Id);
                    }
                    break;
                case ModelConsts.d_emissions:
                    _modelManager.Gens.Outputs(mlp, ModelConsts.d_zone, auxdata, out nmarray);
                    _modelManager.Gens.Outputs(mlp, ModelConsts.d_emissions, auxdata, out tarray);
                    for( i=1; i<tarray.GetLength(0);i++) {
                        znm = (ElsiMainZone) nmarray[i,1];
                        zid = ZoneId(znm);
                        double curVal = oparray[zid,1]!=null ? (double) oparray[zid,1] : 0;
                        oparray[zid,1] = curVal + (double) tarray[i,1];
                    }
                    break;
            }
        }

        // return zone demand constraint def
        // return nothing if zone not found
        public LPConsDef? DemConsDef(ElsiMainZone mainZone) {
            int i;
            i = ZoneId(mainZone);
            if ( i==-1) {
                return null;
            } else {
                return _dcdef[i];
            }
        }

        public int ZoneId(ElsiMainZone mainZone) {
            if ( _data.ZDem.ContainsKey(mainZone)) {
                return _data.ZDem.GetIndex(mainZone);
            } else {
                return 0;
            }
        }

        public double ZoneDemand(LP mlp, ElsiMainZone mainZone) {
            var id = ZoneId(mainZone);
            return -mlp.GetBvec(_dcdef[id].Id);
        }

        public void ZoneMPS(LP mlp, out double[] mps) {
            int i, n;
            n = _dcdef.Length-1;
            mps = new double[n];
            for(i=0;i<n;i++) {
                mps[i] = mlp.Shadow(_dcdef[i+1].Id);
            }
        }

    }
}