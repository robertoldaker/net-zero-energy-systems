using NHibernate.Criterion;
using Npgsql.Replication;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public static class EditItemMethods {
    public static int? GetGenerationModelId(this EditItem editItem)
    {
        if (editItem.data.TryGetValue("_generationModelId", out object value)) {
            var jsonElement = (System.Text.Json.JsonElement)value;
            var tmId = jsonElement.GetInt32();
            return tmId;
        }
        return null;
    }

    public static DatasetData<Generator>  UpdateNodeGeneration(this EditItem editItem, DataAccess da, int datasetId, DatasetData<Node> nodeDi)
    {
        // Update the generation value of a Dataset of nodes
        var tmId = editItem.GetGenerationModelId();
        if (tmId != null) {
            // get generation model
            (var tmDi, var tmeDi) = da.BoundCalc.GetGenerationModelDatasetData(datasetId, m => m.Id == tmId, true);
            var tm = tmDi.Data.Count > 0 ? tmDi.Data[0] : null;
            // get list of generators
            var nodeIds = nodeDi.Data.Select(m => m.Id).ToArray();
            var nodeGenDi = da.BoundCalc.GetNodeGeneratorDatasetData(datasetId, m => m.Node.Id.IsIn(nodeIds));
            var genIds = nodeGenDi.Data.Select(m => m.Generator.Id).ToArray();
            var genDi = da.BoundCalc.GetGeneratorDatasetData(datasetId, m => m.Id.IsIn(genIds));
            // update scaling and set the generation for the generators required
            if (tm != null) {
                tm.UpdateScaling(da, datasetId);
                tm.UpdateGenerators(genDi.Data);
                return genDi;
            } else {
                throw new Exception($"Could not find GenerationModel with id=[{tmId}]");
            }
        } else {
            throw new Exception("GenerationModelId is not set");
        }
    }
}
