VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "MO"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
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
