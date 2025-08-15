
using Microsoft.Extensions.ObjectPool;

namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class GenerationModelEntryItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {
        // autoScaling
        m.CheckBoolean("autoScaling");
        //  scaling
        m.CheckDouble("scaling");
    }

    public override IDatasetIId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id > 0 ? m.Da.BoundCalc.GetGenerationModelEntry(id) : throw new Exception("Unexpected zero id found for GenerationModelEntry");
    }

    public override void Save(EditItemModel m)
    {
        GenerationModelEntry obj = (GenerationModelEntry) m.Item;
        // autoScaling
        var autoScaling = m.CheckBoolean("autoScaling");
        if (autoScaling!=null) {
            obj.AutoScaling = (bool) autoScaling;
        }
        // scaling
        if (!obj.AutoScaling)
        {
            var scaling = m.CheckDouble("scaling");
            if (scaling != null)
            {
                obj.Scaling = (double)scaling;
            }
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess(false) ) {
            var list = new List<DatasetData<object>>();
            var tme = (GenerationModelEntry) m.Item;

            // get generation model
            (var diTM,var diTME) = da.BoundCalc.GetGenerationModelDatasetData(m.Dataset.Id,m => m.Id == tme.GenerationModelId, true);
            var tm = diTM.Data[0];

            tm.UpdateScaling(da, m.Dataset.Id);

            list.Add(diTM.getBaseDatasetData());
            list.Add(diTME.getBaseDatasetData());
            return list;
        }
    }

}
