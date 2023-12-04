VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "DTable"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' Manage model data tables
' L Dale 25 Jan 2019

Option Explicit
Option Base 0

Private mtable As ListObject

' Initialise this table object
' name should be existing excel table/listobject or named range
' reqs is parmarray of required column names.  These are created if they don't exist.
' key is column name of unique row key

Public Sub Init(name As String, reqs As Variant)
    Dim i As Long, f As Long, s As String
    Dim wksht As Worksheet
    Dim lc As ListColumn
    Dim hdrcell As Range
    
    Set mtable = Range(name).ListObject
    If mtable Is Nothing Then
        Set wksht = Range(name).Worksheet
        Set mtable = wksht.ListObjects.Add(xlSrcRange, Range(name).CurrentRegion)
        Application.names(name).Delete
    End If
    
    mtable.name = name
    
    ' check reqs are in HeaderRow
    
    For i = LBound(reqs) To UBound(reqs)
        s = reqs(i)
        f = FindCol(s)
        If f <= 0 Then
            Set lc = mtable.ListColumns.Add
            lc.name = s
        Else
            Set lc = mtable.ListColumns.Item(f)
        End If
        Set hdrcell = lc.Range.Cells(1, 1)
        hdrcell.Font.Italic = True
    Next i
    
End Sub

' Find column with name cname
' Returns -1 if not found

Public Function FindCol(cname As String) As Long
    Dim i As Long
    Dim lc As ListColumn
    
    On Error GoTo errorhandler
        Set lc = mtable.ListColumns.Item(cname)
        FindCol = lc.Index
        Exit Function
    
errorhandler:
    FindCol = -1
End Function

' Return all headers

Public Sub GetHdrs(nms() As Variant)
    nms = mtable.HeaderRowRange
End Sub

' Get all the data from a table

Public Sub GetData(data() As Variant)
    Dim rng As Range
    
    Set rng = mtable.DataBodyRange
    If rng Is Nothing Then
        data = Array()
    Else
        data = rng.Value
    End If
End Sub

' Get a column from a table

Public Sub GetColumn(cname As String, data() As Variant)
    Dim rng As Range
    
    Set rng = mtable.ListColumns(cname).DataBodyRange
    
    If rng Is Nothing Then
        data = Array()
    Else
        data = rng.Value
    End If
End Sub

' Put a column into a table

Public Sub PutColumn(cname As String, data() As Variant)
    Dim rng As Range
    Dim f As Long
    Dim lc As ListColumn
    Dim hdrcell As Range
    
    f = FindCol(cname)
    If f <= 0 Then
        Set lc = mtable.ListColumns.Add
        lc.name = cname
    Else
        Set lc = mtable.ListColumns.Item(f)
    End If
        
    Set hdrcell = lc.Range.Cells(1, 1)
    hdrcell.Font.Italic = True
    
    Set rng = mtable.ListColumns(cname).DataBodyRange
    
    rng.Value = data
End Sub

Public Sub Clear()
    Dim trow As ListRow
    
    While mtable.ListRows.Count > 0
        Set trow = mtable.ListRows.Item(1)
        trow.Delete
    Wend
End Sub

Public Function NewRow() As Long
    
    mtable.ListRows.Add
    NewRow = mtable.ListRows.Count
End Function

Public Sub PutCell(r As Long, cname As String, data As Variant)
    Dim rng As Range
    
    Set rng = mtable.ListColumns(cname).DataBodyRange
    
    If rng Is Nothing Then
        While NewRow() < r
        Wend
        Set rng = mtable.ListColumns(cname).DataBodyRange
    End If
    rng.Cells(r, 1) = data
End Sub


Public Function GetCell(r As Long, cname As String) As Variant
    Dim rng As Range
    
    Set rng = mtable.ListColumns(cname).DataBodyRange
    
    If rng Is Nothing Then
        GetCell = Empty
    Else
        GetCell = rng.Cells(r, 1)
    End If
End Function

Public Function GetDataRange(cname As String) As Range

    Set GetDataRange = mtable.ListColumns(cname).DataBodyRange
End Function
