VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "NodeOrder"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
' Simulate gaussian elimination of network admittance equations
' NB orders all nodes (makes no assumption about reference node - useful for simple short cct calc)
'
' L A Dale 5 July 2020
' 30 Dec 2023 Modified to use nodes & branches collections

Option Explicit
Option Base 0

Private Const BPL As Long = 31  ' useable bits per long

Public nz As Long               ' Original non-zeros (for upper diagonal only)
Public fz As Long               ' Final nz count
Public nn As Long               ' Upper bound of connected nodes
Private nord() As Long          ' Order position (0..n) of node index i (i=0..n)
Private npos() As Long          ' Position of ith node
Private bitmap() As Long        ' Bit per nz element


Sub Init()
    Dim nw As Long
    Dim n1 As Long, n2 As Long
    Dim minc As Long, mini As Long
    Dim rowbits() As Long
    Dim i As Long, j As Long, t As Long
    Dim m As Long, w As Long
    Dim br As Branch
    
    
    nn = nodes.Count - 1   ' assume all nodes ac connected
    nw = nn \ BPL
    
    ReDim bitmap(nn, nw) As Long
    ReDim rowbits(nn) As Long
    ReDim nord(nn) As Long
    ReDim npos(nn) As Long
    
    ' Mark non-zero elements in bit map
    For Each br In branches
        If br.Xval <> 0# Then
            n1 = br.node1.Index - 1
            n2 = br.node2.Index - 1
        
            bitmap(n1, MWord(n1)) = bitmap(n1, MWord(n1)) Or Mask(n1)
            bitmap(n1, MWord(n2)) = bitmap(n1, MWord(n2)) Or Mask(n2)
            bitmap(n2, MWord(n2)) = bitmap(n2, MWord(n2)) Or Mask(n2)
            bitmap(n2, MWord(n1)) = bitmap(n2, MWord(n1)) Or Mask(n1)
        End If
    Next br
            
    ' Count non-zero elements
    nz = 0
    For i = 0 To nn
        nord(i) = i
        For j = 0 To nw
            rowbits(i) = rowbits(i) + CountBits(bitmap(i, j))
        Next j
        nz = nz + rowbits(i)
     Next i
     
     ' move unconnected nodes to end
     
     i = 0
     While i <= nn
        If rowbits(nord(i)) = 0 Then
            If i <> nn Then
                t = nord(i)
                nord(i) = nord(nn)
                nord(nn) = t
            End If
            nn = nn - 1
        Else
            i = i + 1
        End If
     Wend
     
     nz = (nz - nn - 1) / 2 + nn + 1  ' Adjust for upper diagonal only
    
     For i = 0 To nn - 1      ' Each column except last (where there is no choice)
     
        ' find a pivot with fewest non-zeros
        minc = rowbits(nord(i))
        mini = i
        For j = i + 1 To nn   ' Look for unused row with least non-zeros
            If rowbits(nord(j)) < minc Then
                minc = rowbits(nord(j))
                mini = j
            End If
        Next j
            
        If mini <> i Then
            t = nord(i)
            nord(i) = nord(mini)
            nord(mini) = t
        End If
        
        ' bits to be eliminated, the column corresponding to the pivot row
        
        w = MWord(nord(i))
        m = Mask(nord(i))
        
        ' simulate gaussian elimination of all non-zeros in column of pivot
        For j = i + 1 To nn
            If bitmap(nord(j), w) And m Then
                rowbits(nord(j)) = SimSubRow(nord(i), nord(j)) - 1
                bitmap(nord(j), w) = bitmap(nord(j), w) Xor m
            End If
        Next j
    Next i
    
    'Total final non zeros and mapping from node row to order number
    fz = 0
    For i = 0 To nodes.Count - 1
        npos(nord(i)) = i
        fz = fz + rowbits(i)
    Next i
    
    Erase bitmap
End Sub

Private Function Mask(n As Long) As Long
    Dim nn As Long
    
    nn = n Mod BPL
    Mask = 2 ^ nn
End Function

Private Function MWord(n As Long) As Long
    MWord = n \ BPL
End Function

Private Function CountBits(ByVal v As Long) As Long
    Dim i As Long, r As Long
    
    For i = 0 To BPL - 1
        If (v And Mask(i)) <> 0 Then
            r = r + 1
        End If
    Next i
    CountBits = r
End Function

' Simulate gaussian elimination subtract piv row p from target row t returns nzcount

Private Function SimSubRow(ByVal p As Long, ByVal t As Long) As Long
    Dim nz As Long
    Dim i As Long
    
    For i = 0 To UBound(bitmap, 2)
        bitmap(t, i) = bitmap(t, i) Or bitmap(p, i)
        
        nz = nz + CountBits(bitmap(t, i))
    Next i
    
    SimSubRow = nz
End Function

' Return node table row index row corresponding to loadflow position p

Public Function NodeId(p As Long) As Long
    NodeId = nord(p) + 1
End Function

' Find position of node n (the row in Node table)

Public Function NodePos(nid As Long) As Long
    NodePos = npos(nid - 1)
End Function
