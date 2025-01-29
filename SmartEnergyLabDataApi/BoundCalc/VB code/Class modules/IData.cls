VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "IData"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' Interface for data item

Option Explicit

Public Property Get name() As String
End Property

Public Function FieldMap(mtable As DTable, map() As Long) As Boolean
End Function

Public Function Init(Data() As Variant, row As Long, map() As Long) As Boolean
End Function

