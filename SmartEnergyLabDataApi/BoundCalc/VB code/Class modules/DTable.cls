VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "DTable"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' An input data table
' Version 2 9 January 2024
' Collect places data in Excel listobject into a collection of objects which implement IData interface
' While input and output is notionally listobject row orientated, the implementation addresses Excel performance issues by reducing spreadsheet reads/writes
' Putcolumn is a convenient and efficient way of adding output to an existing table

Option Explicit

Private mtable As ListObject

Public Function Init(name As String) As Boolean
    
    On Error GoTo errorhandler
    
    Set mtable = Range(name).ListObject
    Init = True
    Exit Function
    
errorhandler:
    Init = False
End Function

Public Function RowCount() As Long
    Dim rng As Range
    
    Set rng = mtable.DataBodyRange
    
    If rng Is Nothing Then
        RowCount = 0
    Else
        RowCount = rng.rows.Count
    End If
End Function

' Find the field column number

Public Function Field(fname As String) As Long
    Dim lc As ListColumn
    
    On Error GoTo errorhandler
        
    Set lc = mtable.ListColumns.item(fname)
    Field = lc.Index
        
    Exit Function
    
errorhandler:
    Field = 0
End Function

Public Function FieldMap(map() As Long, fieldnames As Variant, Optional create As Boolean = False) As Boolean
    Dim i As Long, fc As Long
    Dim fname As String
    Dim res As Boolean
    Dim lc As ListColumn
    
    res = True
    fc = UBound(fieldnames)
    
    ReDim map(fc) As Long
    
    For i = 0 To fc
        fname = fieldnames(i)
        map(i) = Field(fname)
        
        If map(i) = 0 Then
            If create Then
                Set lc = mtable.ListColumns.Add
                lc.name = fname
                map(i) = lc.Index
            Else
                MsgBox "Field " & fieldnames(i) & " not found in table " & mtable.name
                res = False
            End If
        End If
    Next i
    
    FieldMap = res
End Function


Public Sub GetData(darray() As Variant)
    Dim rng As Range
    
    Set rng = mtable.DataBodyRange()
    
    If Not rng Is Nothing Then
        darray = rng
    End If
End Sub

Public Sub PutData(darray() As Variant)
    Dim rng As Range
    
    Set rng = mtable.DataBodyRange()
    
    If Not rng Is Nothing Then
        If (mtable.ListRows.Count = UBound(darray, 1) - LBound(darray, 1) + 1) And _
           (mtable.ListColumns.Count >= UBound(darray, 2) - LBound(darray, 2) + 1) Then
            rng = darray
        End If
    End If
End Sub

Public Function Collect(coll As Collection, ttype As Long) As Boolean
    Dim it As IData
    Dim fields() As Long, Data() As Variant
    Dim i As Long, fieldsinit As Boolean
    
'    On Error GoTo errorhandler
    Collect = True
    
    For i = 1 To RowCount()
        Set it = NewItem(ttype)
        
        If Not fieldsinit Then
            fieldsinit = True
            Collect = it.FieldMap(Me, fields)
            If Not Collect Then
                Exit Function
            End If
            GetData Data
        End If
        
        Collect = it.Init(Data, i, fields)
        
        If Exists(it.name, coll) Then
            MsgBox "Duplicate item name " & it.name
            Collect = False
        Else
            coll.Add it, it.name
        End If
    Next i
    
    Exit Function
        
errorhandler:
    MsgBox "Error reading data from " & mtable.name
    
    Collect = False
End Function

' Clear rows

Public Sub ClearRows()
    Dim r As ListRow, i As Long
    
    With mtable.ListRows
    While .Count > 0
        Set r = .item(.Count)
        r.Delete
    Wend
    End With
End Sub

' New Row in Table

Public Sub NewRow(map() As Long, ParamArray Data() As Variant)
    Dim r As ListRow
    Dim i As Long
    Dim rng As Range, rdat() As Variant
    
    Set r = mtable.ListRows.Add
    Set rng = r.Range
    rdat = rng
    
    For i = 0 To UBound(map)
        rdat(1, map(i)) = Data(i)
    Next i
    rng = rdat
End Sub

' Populate row in darray

Public Sub PopulateRow(darray() As Variant, r As Long, map() As Long, ParamArray Data() As Variant)
    Dim i As Long
    
    For i = 0 To UBound(map)
        darray(r, map(i)) = Data(i)
    Next i
End Sub

' Resize table

Public Sub Resize(rows As Long)
'    Dim hrng As Range
'    Dim drng As Range
'
'    If rows < 1 Then
'        rows = 1
'    End If
'    Set hrng = mtable.HeaderRowRange
'    Set drng = hrng.Resize(rows + 1)
'
'    mtable.Resize drng
    Dim lrs As ListRows
    Dim lr As ListRow
    
    Set lrs = mtable.ListRows
    
    While lrs.Count < rows
        lrs.Add
    Wend
    
    While lrs.Count > rows
        Set lr = lrs.item(lrs.Count)
        lr.Delete
    Wend
    
End Sub


' Put a column into a table, data must be required number of rows with 1 column

Public Sub PutColumn(cname As String, Data() As Variant)
    Dim rng As Range
    Dim f As Long
    
    Dim lc As ListColumn
    
    f = Field(cname)
    If f <= 0 Then
        Set lc = mtable.ListColumns.Add
        lc.name = cname
    Else
        Set lc = mtable.ListColumns.item(f)
    End If
    
    Set rng = lc.DataBodyRange
    
    If Not rng Is Nothing Then
        rng.Value = Data
    End If
End Sub

Public Sub ClearColumn(cname As String)
    Dim rng As Range
    Dim f As Long
    
    Dim lc As ListColumn
    
    f = Field(cname)
    If f <= 0 Then
        Set lc = mtable.ListColumns.Add
        lc.name = cname
    Else
        Set lc = mtable.ListColumns.item(f)
    End If
    
    Set rng = lc.DataBodyRange
    
    rng.Value = Empty
End Sub
