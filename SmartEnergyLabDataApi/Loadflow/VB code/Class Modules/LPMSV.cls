VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "LPMSV"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
' Multi segment variable description
Option Explicit
Option Base 0

Public name As String   ' used as key to find msv
Public mmo As MO        ' the merit order handler
Public pos As Long      ' current segment
Public vid As Long      ' variable index in LP
Public zcid As Long     ' zero constraint in LP
Public mcid As Long     ' max constraint in LP
Public dcid As Long     ' the demand or auxiliary constraint

