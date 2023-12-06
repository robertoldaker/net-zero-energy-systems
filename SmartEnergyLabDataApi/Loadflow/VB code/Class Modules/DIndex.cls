VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "DIndex"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' Create an index of an array

Option Explicit
Option Base 0

Private ind As Collection

' Returns index of first duplicate or ubound+1 if none

Public Function MkIndex(data() As Variant, kc As Long) As Long
    Dim i As Long
    
    Set ind = New Collection
    
    On Error GoTo errorhandler
    For i = LBound(data, 1) To UBound(data, 1)
        ind.Add i, data(i, kc)
    Next i
    MkIndex = i
    Exit Function
    
errorhandler:
    MkIndex = i
End Function

Public Sub Init()
    Set ind = New Collection
End Sub
Public Function AddKey(n As Long, key As Variant) As Boolean
    
    On Error GoTo errorhandler
    ind.Add n, key
    
    AddKey = True
    Exit Function
errorhandler:

    AddKey = False
End Function

Public Function Lookup(rk As Variant) As Long
    
    On Error GoTo errorhandler
    Lookup = ind.Item(rk)
    Exit Function
    
errorhandler:
    Lookup = ind.Item(ind.Count) + 1
End Function

Public Function Count() As Long
    Count = ind.Count
End Function
