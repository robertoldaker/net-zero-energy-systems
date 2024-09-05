namespace SmartEnergyLabDataApi.Data;

public class GenParameterItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {

    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        return new List<DatasetData<object>>();
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        var obj = id>0 ? m.Da.Elsi.GetGenParameter(id) : new GenParameter(m.Dataset);
        if ( obj==null ) {
            throw new Exception($"Cannot find gen parameter with id=[{id}]");
        }
        return obj;
    }

    public override void Save(EditItemModel m)
    {
    }
}