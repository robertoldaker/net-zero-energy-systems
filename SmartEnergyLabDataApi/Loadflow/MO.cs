/*
' Merit order object
'
' L Dale 21 Nov 2017
' Updated for private collection 4 Feb 2019
'

Option Explicit
Option Base 0

Private segs As New Collection  ' of array(name, id, cost, size) with key=name
Private ord() As Long
Private siz() As Double
Private cst() As Double
Private sorted As Boolean

Public Sub Add(name As String, cost As Double, size As Double)
    Dim idno As Long
    
    idno = segs.Count
    segs.Add Array(name, idno, cost, size), name
    
    sorted = False
End Sub


Public Function SegExists(name As String) As Boolean
    Dim seg() As Variant
    
    On Error GoTo errorhandler
        seg = segs.Item(name)
        SegExists = True
    Exit Function
    
errorhandler:
    SegExists = False
End Function

Public Sub Remove(name As String)
    segs.Remove name
    sorted = False
End Sub

Public Sub Update(name As String, cost As Double, size As Double)
    Dim seg() As Variant
    
    seg = segs.Item(name)
    segs.Remove name
    seg(2) = cost
    seg(3) = size
    segs.Add seg, name
    
    sorted = False
End Sub

Public Sub Details(name As String, ByRef cost As Double, ByRef size As Double)
    Dim seg() As Variant
    
    seg = segs.Item(name)
    cost = seg(2)
    size = seg(3)
End Sub

Public Function Count() As Long
    Count = segs.Count
End Function

' Copy this merit order into another

Public Sub Copy(tomo As MO)
    Dim seg As Variant
    Dim nm As String, cst As Double, sz As Double
    
    For Each seg In segs
        nm = seg(0)
        cst = seg(2)
        sz = seg(3)
        tomo.Add nm, cst, sz
    Next seg
End Sub

Private Sub Order()
    Dim c As Long, i As Long
    Dim seg() As Variant
    
    c = segs.Count - 1
    If c >= 0 Then
        ReDim ord(c)
        ReDim cst(c)
        ReDim siz(c)
    
        For i = 0 To c
            seg = segs.Item(i + 1)
            cst(i) = seg(2)
            siz(i) = seg(3)
            ord(i) = i
        Next i
    
        MergeSortFlt cst, ord, c + 1
    End If
    sorted = True
End Sub
  

' Schedule to meet demand
'
' returns order position or -1 if no segments
' returns slack which will be negative if volume not achievable

Public Function Vschedule(volume As Double, ByRef Slack As Double) As Long
    Dim i As Long, j As Long, c As Long
    Dim b As Double, s As Double
    
    If Not sorted Then
        Order
    End If
    
    c = segs.Count - 1
    b = 0#

    For i = 0 To c
        j = ord(i)
        s = siz(j)
        
        If b + s >= volume Then
            Exit For
        End If
        
        b = b + s
    Next i
    
    If i > c Then
        i = c
    End If
    
    Vschedule = i
    Slack = s - (volume - b)  ' might be negative
End Function

' Return the size of all segments below pos

Public Function Base(pos As Long) As Double
    Dim b As Double
    Dim i As Long, j As Long
    
    If Not sorted Then
        Order
    End If
    
    For i = 0 To pos - 1
        j = ord(i)
            
        b = b + siz(j)
    Next i
    
    Base = b
End Function

' Return the cost of all segments below pos
' Returns 0 if pos unset

Public Function BaseCost(pos As Long) As Double
    Dim i As Long, j As Long
    Dim c As Double
    
    If Not sorted Then
        Order
    End If
    
    For i = 0 To pos - 1
        j = ord(i)
        c = c + siz(j) * cst(j)
    Next i
    
    BaseCost = c
End Function

' Return segment details

Public Sub PosDetails(pos As Long, ByRef name As String, ByRef cost As Double, ByRef size As Double)
    Dim j As Long, c As Long
    Dim seg() As Variant

    If Not sorted Then
        Order
    End If
    
    c = segs.Count - 1
    If pos >= 0 And pos <= c Then
        j = ord(pos)
    
        seg = segs.Item(j + 1)
        name = seg(0)
        cost = seg(2)
        size = seg(3)
    Else
        name = ""
        cost = 0#
        size = 0#
    End If
End Sub


' Find segment of cost equal or greater than specified price
' If no segment costs are greater than price then return highest

Public Function Pschedule(price As Double) As Long
    Dim i As Long, j As Long, c As Long
    Dim p As Long
    
    If Not sorted Then
        Order
    End If
    
    c = segs.Count
    p = c - 1
    
    For i = c - 1 To 0 Step -1
        j = ord(i)
        If cst(j) >= price Then
            p = i
        Else
            Exit For
        End If
    Next i
    
    Pschedule = p
End Function

Public Function IsFirst(pos As Long) As Boolean
    IsFirst = (pos = 0)
End Function

Public Function IsLast(pos As Long) As Boolean
    IsLast = (pos = segs.Count - 1)
End Function

Public Function Up(pos As Long) As Long
    If Not IsLast(pos) Then
        Up = pos + 1
    Else
        Up = pos
    End If
        
End Function

Public Function Down(pos As Long) As Long
    If Not IsFirst(pos) Then
        Down = pos - 1
    Else
        Down = pos
    End If
End Function

Public Function CostDown(pos As Long) As Double
    
    If Not sorted Then
        Order
    End If
    
    CostDown = cst(Down(pos))
End Function

Public Function CostUp(pos As Long) As Double

    If Not sorted Then
        Order
    End If
    
    CostUp = cst(Up(pos))
End Function

' Provide a supply curve of the merit order

Public Sub Scurve(ByRef xv() As Variant, ByRef yv() As Variant, ByRef nms() As String)
    Dim c As Long
    Dim i As Long, j As Long
    Dim s As Double
    Dim seg() As Variant
    
    If Not sorted Then
        Order
    End If
    
    c = segs.Count
    
    ReDim xv(1 To c * 2) As Variant
    ReDim yv(1 To c * 2) As Variant
    ReDim nms(1 To c) As String
    
    For i = 0 To c - 1
        j = 2 * i + 1
        xv(j) = s
        yv(j) = cst(ord(i))
        s = s + siz(ord(i))
        xv(j + 1) = s
        yv(j + 1) = yv(j)
        seg = segs.Item(ord(i) + 1)
        nms(i + 1) = seg(0)
    Next i
End Sub

' Determine whether a segment is in_merit=1, marginal=0 or out_of_merit=-1

Public Function Dispatch(name As String, pos As Long) As Long
    Dim seg() As Variant
    Dim sop As Long
    
    If Not sorted Then
        Order
    End If
    
    seg = segs.Item(name)
    sop = ord(seg(1))
    
    If sop < pos Then
        Dispatch = 1
    ElseIf sop = pos Then
        Dispatch = 0
    Else
        Dispatch = -1
    End If
End Function

Public Function Test() As Boolean
    Dim pos As Long, sl As Double
    Dim nm As String, cst As Double, sz As Double
    
'    Init
    
    Me.Add "a", 10, 100
    Me.Add "b", 20, 200
    Me.Add "c", 30, 300
    
    Me.Update "b", 25, 250
    
    pos = Me.Vschedule(200, sl)
    Me.PosDetails pos, nm, cst, sz
    
    Test = (nm = "b") And (sl = 150#)
End Function

*/
using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Loadflow
{
    public class MO
    {
        private Collection<object[]> segs;
        private int[] ord;
        private double[] siz;
        private double[] cst;
        private bool sorted;
        public MO()
        {
            segs = new Collection<object[]>();
        }

        public void Add(string name, double cost, double size)
        {
            int idno;
            idno = segs.Count;
            segs.Add(new object[] { name, idno, cost, size }, name);
            sorted = false;
        }

        public bool SegExists(string name)
        {
            return segs.ContainsKey(name);
        }

        public void Remove(string name)
        {
            segs.Remove(name);
            sorted = false;
        }

        public void Update(string name, double cost, double size)
        {
            var seg = segs.Item(name);
            segs.Remove(name);
            seg[2] = cost;
            seg[3] = size;
            segs.Add(seg, name);
            sorted = false;
        }

        public void Details(string name, ref double cost, ref double size)
        {
            var seg = segs.Item(name);
            cost = (double)seg[2];
            size = (double)seg[3];
        }

        public int Count()
        {
            return segs.Count;
        }

        // Copy this merit order into another
        public void Copy(MO tomo)
        {
            string nm;
            double cst, sz;
            foreach (var seg in segs.Items)
            {
                nm = (string)seg[0];
                cst = (double)seg[2];
                sz = (double)seg[3];
                tomo.Add(nm, cst, sz);
            }
        }

        public void Order()
        {
            int c, i;
            c = segs.Count - 1;
            object[] seg;

            if (c >= 0)
            {
                ord = new int[c + 1];
                cst = new double[c + 1];
                siz = new double[c + 1];

                for (i = 0; i <= c; i++)
                {
                    seg = segs.Item(i + 1);
                    cst[i] = (double)seg[2];
                    siz[i] = (double)seg[3];
                    ord[i] = i;
                }

                LPhdr.MergeSortFlt(cst, ord, c + 1);
            }
            sorted = true;
        }

        // Schedule to meet demand
        //
        // returns order position or -1 if no segments
        // returns slack which will be negative if volume not achievable
        public int Vschedule(double volume, ref double Slack)
        {
            int i, j, c;
            double b, s = 0;
            if (!sorted)
            {
                Order();
            }
            c = segs.Count - 1;
            b = 0;
            for (i = 0; i <= c; i++)
            {
                j = ord[i];
                s = siz[j];
                if (b + s >= volume)
                {
                    break;
                }
                b = b + s;
            }

            if (i > c)
            {
                i = c;
            }
            Slack = s - (volume - b); // might be negative
            return i;
        }

        // Return the size of all segments below pos
        public double Base(int pos)
        {
            double b = 0;
            int i, j;
            if (!sorted)
            {
                Order();
            }
            for (i = 0; i < pos; i++)
            {
                j = ord[i];
                b = b + siz[j];
            }
            return b;
        }

        // Return the cost of all segments below pos
        // Returns 0 if pos unset
        public double BaseCost(int pos)
        {
            int i, j;
            double c = 0;
            if (!sorted)
            {
                Order();
            }
            for (i = 0; i < pos; i++)
            {
                j = ord[i];
                c = c + siz[j] * cst[j];
            }
            return c;
        }

        // Return segment details
        public void PosDetails(int pos, ref string name, ref double cost, ref double size)
        {
            int j,c;
            object[] seg;
            if (!sorted) {
                Order();
            }
            c = segs.Count-1;
            if ( pos>=0 && pos <=c ) {
                j=ord[pos];
                seg = segs.Item(j+1);
                name=(string) seg[0];
                cost=(double) seg[2];
                size = (double) seg[3];
            } else {
                name = "";
                cost = 0;
                size = 0;
            }
        }

        // Find segment of cost equal or greater than specified price
        // If no segment costs are greater than price then return highest
        public int Pschedule(double price) 
        {
            int i,j,c,p;
            if ( !sorted) {
                Order();
            }

            c = segs.Count;
            p = c-1;
            for(i=c-1;i>=0;i--) {
                j = ord[i];
                if ( cst[j] >= price) {
                    p = i;
                } else {
                    break;
                }
            }

            return p;
        }

        public bool IsFirst(int pos) {
            return pos==0;
        }

        public bool IsLast(int pos) {
            return (pos == segs.Count-1);
        }

        public int Up(int pos) {
            int result;
            if (!IsLast(pos)) {
                result = pos +1;
            } else {
                result = pos;
            }
            return result;
        }

        public int Down(int pos) {
            int result;
            if (!IsFirst(pos)) {
                result = pos-1;
            } else {
                result = pos;
            }
            return result;
        }

        public double CostDown(int pos) {
            if ( !sorted) {
                Order();
            }
            return cst[Down(pos)];
        }

        public double CostUp(int pos) {
            if ( !sorted) {
                Order();
            }
            return cst[Up(pos)];
        }

        public int Dispatch(string name, int pos) {
            object[] seg;
            int sop;
            if ( !sorted) {
                Order();
            }
            seg = segs.Item(name);
            sop = ord[(int)seg[1]];
            int result;
            if ( sop<pos) {
                result = 1;
            } else if ( sop==pos) {
                result = 0;
            } else {
                result = -1;
            }
            return result;
        }

        public bool Test() {
            int pos;
            double sl=0, cst=0, sz=0;
            string nm="";
            this.Add("a",10,100);
            this.Add("b",20,200);
            this.Add("c",30,300);
            this.Update("b",25,250);
            pos = this.Vschedule(200,ref sl);
            this.PosDetails(pos,ref nm,ref cst,ref sz);

            return (nm=="b") && (sl==150);
        }
    }
}