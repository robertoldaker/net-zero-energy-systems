using NHibernate.Criterion;
using Npgsql.Replication;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public static class EditItemMethods {
    public static int? GetTransportModelId(this EditItem editItem)
    {
        if (editItem.data.TryGetValue("_transportModelId", out object value)) {
            //var valueStr = (string)value;
            //int transportModelId;
            //if (int.TryParse(valueStr, out transportModelId)) {
            //    return transportModelId;
            //}
            var jsonElement = (System.Text.Json.JsonElement)value;
            var tmId = jsonElement.GetInt32();
            return tmId;
        }
        return null;
    }

    public static DatasetData<Generator>  UpdateNodeGeneration(this EditItem editItem, DataAccess da, int datasetId, DatasetData<Node> nodeDi)
    {
        // Update the generation value of a Dataset of nodes
        var tmId = editItem.GetTransportModelId();
        if (tmId != null) {
            // get transport model
            var tmDi = da.BoundCalc.GetTransportModelDatasetData(datasetId, m => m.Id == tmId, true);
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
                throw new Exception($"Could not find transportModel with id=[{tmId}]");
            }
        } else {
            throw new Exception("TransportModelId is not set");
        }
    }
}
