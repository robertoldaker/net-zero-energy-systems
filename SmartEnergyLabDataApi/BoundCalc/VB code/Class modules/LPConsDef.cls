VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "LPConsDef"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Defines a LP Model Constraint

Option Explicit
Option Base 0

Public name As String
Public Id As Long
Public CType As Long        ' CTLTE, CTGTE, or CTEQ
Public Magnitude As Double
Public Pairs As Variant


Public Sub augment(varname As String, coefficient As Double)
    Dim u As Long
        
    u = UBound(Pairs)
    
    ReDim Preserve Pairs(u + 2)
    Pairs(u + 1) = varname
    Pairs(u + 2) = coefficient
End Sub

Public Function Slack(mlp As LP) As Double
    Slack = mlp.Slack(Id)
End Function

Public Function Shadow(mlp As LP) As Double
    Shadow = mlp.Shadow(Id)
End Function
