
namespace SmartEnergyLabDataApi.Data.BoundCalc;


public class GeneratorItemHandler : BaseEditItemHandler
{
    public override void Check(EditItemModel m)
    {
        // name
        if ( m.GetString("name",out string name)) {
            if ( m.Da.BoundCalc.GeneratorExists(m.Dataset.Id,name, out Dataset? dataset) ) {
                m.AddError("name",$"Generator already exists with name [{name}] in dataset [{dataset?.Name}]");
            }
        } else if ( m.ItemId==0) {
            m.AddError("name","Name must be set to something");
        }

        // capacity
        m.CheckDouble("capacity");
    }

    public override IId GetItem(EditItemModel m)
    {
        var id = m.ItemId;
        return id>0 ? m.Da.BoundCalc.GetGenerator(id) : new Generator(m.Dataset);
    }

    public override void Save(EditItemModel m)
    {
        Generator gen = (Generator) m.Item;
        //
        if ( m.GetString("name",out string name)) {
            gen.Name = name;
        }
        //
        if ( gen.Id==0) {
            m.Da.BoundCalc.Add(gen);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();  
            var gen = (Generator) m.Item;          
            var genDi = da.BoundCalc.GetGeneratorDatasetData(m.Dataset.Id, m=>m.Id == gen.Id);
            list.Add(genDi.getBaseDatasetData());
            return list;
        }
    }

}

