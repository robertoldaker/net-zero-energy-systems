
using Microsoft.Extensions.ObjectPool;

namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class TransportModelEntryItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {
        // autoScaling
        m.CheckBoolean("autoScaling");
        //  scaling
        m.CheckDouble("scaling");
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id > 0 ? m.Da.BoundCalc.GetTransportModelEntry(id) : throw new Exception("Unexpected zero id found for TransportModelEntry");
    }

    public override void Save(EditItemModel m)
    {
        TransportModelEntry obj = (TransportModelEntry) m.Item;
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
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var tme = (TransportModelEntry) m.Item;

            // get transport model
            var diTM = da.BoundCalc.GetTransportModelDatasetData(m.Dataset.Id,m => m.Id == tme.TransportModelId);
            var tm = diTM.Data[0];
            // this should be all sibling entries
            var diTME = da.BoundCalc.GetTransportModelEntryDatasetData(m.Dataset.Id, m=>m.TransportModel.Id == tm.Id);

            // get nodes
            var nodeQuery = da.Session.QueryOver<Node>();
            var diNode = new DatasetData<Node>(da, m.Dataset.Id, m => m.Id.ToString(), nodeQuery);

            // get node generators
            var nodeGenQuery = da.Session.QueryOver<NodeGenerator>();
            var diNodeGen = new DatasetData<NodeGenerator>(da, m.Dataset.Id, m => m.Id.ToString(), nodeGenQuery);

            // get generators
            var genQuery = da.Session.QueryOver<Generator>();
            var diGen = new DatasetData<Generator>(da, m.Dataset.Id, m => m.Id.ToString(), genQuery);

            // update scaling
            tm.UpdateScaling(diNode.Data, diNodeGen.Data, diGen.Data);

            list.Add(diTM.getBaseDatasetData());
            list.Add(diTME.getBaseDatasetData());
            return list;
        }
    }

}
