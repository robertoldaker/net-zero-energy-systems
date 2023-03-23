/*
' Interface for master problem part


Option Explicit
Option Base 0


' Populate master problem variable definition

Public Sub VarDefs(vlist() As Variant)

End Sub

' Output variables

Public Sub VarVals(dlp As LP, vvars() As Double)

End Sub

*/
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public interface IMaster {
        // Populate master problem variable definition
        public void VarDefs(object[] vlist);

        // Output variables
        public void VarVals(LP dlp, double[] vvars);
    }
}