
using Microsoft.Extensions.ObjectPool;

namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class TransportModelItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {
        // name
        if (m.GetString("name", out string name)) {
            if (m.Da.BoundCalc.TransportModelExists(m.Dataset.Id, name, out Dataset? dataset)) {
                m.AddError("name", $"Transport model already exists with name [{name}] in dataset [{dataset?.Name}]");
            }
        } else if (m.ItemId == 0) {
            m.AddError("name", "Name must be set to something");
        }
    }

    public override IDatasetIId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        if (id > 0) {
            return m.Da.BoundCalc.GetTransportModel(id);
        } else {
            var tm = new TransportModel(m.Dataset);
            return tm;
        }
    }

    public override void Save(EditItemModel m)
    {
        TransportModel obj = (TransportModel) m.Item;
        //
        if ( m.GetString("name",out string name)) {
            obj.Name = name;
        }
        //
        if (obj.Id == 0) {
            m.Da.BoundCalc.Add(obj);
            // add entries - one for each generator type
            foreach (var gt in Enum.GetValues<GeneratorType>()) {
                var tme = new TransportModelEntry(obj,m.Dataset);
                tme.AutoScaling = true;
                tme.GeneratorType = gt;
                m.Da.BoundCalc.Add(tme);
            }
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var tm = (TransportModel) m.Item;
            // the trnasport model we have just created
            var di = da.BoundCalc.GetTransportModelDatasetData(m.Dataset.Id, m=>m.Id == tm.Id, true);
            // the entries for this transport model
            var diTME = da.BoundCalc.GetTransportModelEntryDatasetData(m.Dataset.Id, m=>m.TransportModel.Id == tm.Id);

            // update the scaling for the transport model
            tm = di.Data[0]; // important to update the reference to transport model
            tm.UpdateScaling(da, m.Dataset.Id);

            // return list of objects that have changed
            list.Add(di.getBaseDatasetData());
            list.Add(diTME.getBaseDatasetData());
            return list;
        }
    }

}
