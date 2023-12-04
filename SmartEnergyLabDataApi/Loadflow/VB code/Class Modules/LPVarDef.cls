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

Option Explicit
Option Base 0

Public name As String
Public Id As Long
Public cost As Double
Public InitC As String  'name of constraint which provides initial value
Public MOmgr As MO      'multisegment data
Public Ipos As Long     'initial segment
Public vzc As String    'name of the zero constraint on variable
Public vmc As String    'name of maximum constraint on variable
Public vdc As String    'name of auxiliary/demand constraint
' Public lunb As Boolean  'true if last segment is unbounded

