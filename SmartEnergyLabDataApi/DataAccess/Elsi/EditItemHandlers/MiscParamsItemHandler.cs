namespace SmartEnergyLabDataApi.Data;

public class MiscParamsItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {

    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        return new List<DatasetData<object>>();
    }

    public override IDatasetIId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        var obj = id>0 ? m.Da.Elsi.GetMiscParams() : new MiscParams(m.Dataset);
        if ( obj==null ) {
            throw new Exception($"Cannot find misc params with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}
