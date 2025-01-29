VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "LPVarDef"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Defines a LPModel Variable
'
' 17 Nov 2023 - removed multisegment variable

Option Explicit
Option Base 0

Public name As String
Public Id As Long
Public Welfare As Double   'actually welfare since +ve values maximised
Public InitC As String  'name of constraint which provides initial value
Public Vzc As LPConsDef 'the zero constraint on variable
Public Vmc As LPConsDef 'the maximum constraint on variable
Public Vpv As LPVarDef  'the positive of a variable pair
Public Vnv As LPVarDef  'the negative of a variable pair

' Calculate the value of the variable from the slack on the zero constraint

Public Function Value(mlp As LP) As Double
    
    If Vpv Is Nothing Then
        Value = mlp.Slack(Vzc.Id)
    Else
        Value = Vpv.Value(mlp) - Vnv.Value(mlp)
    End If
    
End Function
