/*' Interface for a model part

Option Explicit
Option Base 0

' Partname is used as the key to find part in a collection

Public PartName As String

' Build the model part into a LPModel
' dtable is the name of an excel table
' csel is a column selector (e.g. period name)

Public Sub Build(lpm As LPModel, dtab As DTable, Optional csel As Variant)

End Sub

' Update model parameters in the resulting LP
' should use same dtable as Build

Public Sub Update(mlp As LP, Optional csel As Variant)

End Sub

' Set the model ready for a phase of the solution

Public Sub SetPhase(mlp As LP, phaseid As Long, auxdata() As Variant)

End Sub

' Initialise the LP based on a system marginal price

Public Sub Initialise(mlp As LP, smp() As Variant)

End Sub

' Provide outputs

Public Sub Outputs(mlp As LP, dtype As Long, auxdata() As Variant, oparray() As Variant)

End Sub
*/
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Loadflow;

namespace SmartEnergyLabDataApi.Elsi
{
    public interface IPart {

        // Partname is used as the key to find part in a collection
        public string PartName {get; set;}

        // Build the model part into an LPModel
        public void Build(ModelManager modelManager, LPModel lpm, ElsiPeriod period=ElsiPeriod.Pk);

        // Update model parameters in the resulting LP
        public void Update(LP mlp, ElsiPeriod? period=null);

        // Set the model ready for a phase of the solution
        public void SetPhase(LP mlp, int phaseid, object[,] auxdata);

        // Initialise the LP based on a system marginal price
        public void Initialise(LP mlp, double[,] smp);

        // Provide outputs
        public void Outputs(LP mlp, int dtype, double[,] auxdata, out object[,] oparray);

    } 
}
