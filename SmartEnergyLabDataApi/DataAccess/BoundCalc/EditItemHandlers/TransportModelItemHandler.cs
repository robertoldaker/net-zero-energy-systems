
namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class TransportModelItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {
        // name
        if ( m.GetString("name",out string name)) {
            if ( m.Da.BoundCalc.TransportModelExists(m.Dataset.Id,name, out Dataset? dataset) ) {
                m.AddError("name",$"Transport model already exists with name [{name}] in dataset [{dataset?.Name}]");
            }
        } else if ( m.ItemId==0) {
            m.AddError("name","Name must be set to something");
        }
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.BoundCalc.GetTransportModel(id) : new TransportModel(m.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        TransportModel obj = (TransportModel) m.Item;
        //
        if ( m.GetString("name",out string name)) {
            obj.Name = name;
        }
        //
        if ( obj.Id==0) {
            m.Da.BoundCalc.Add(obj);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();  
            var obj = (TransportModel) m.Item;          
            var di = da.BoundCalc.GetTransportModelDatasetData(m.Dataset.Id, m=>m.Id == obj.Id);
            list.Add(di.getBaseDatasetData());
            return list;
        }
    }

}