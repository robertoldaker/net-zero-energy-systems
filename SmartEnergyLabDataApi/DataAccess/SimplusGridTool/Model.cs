using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.SGT
{
    //[Class(0, Table = "SGT_Models")]
    public class Model
    {
        //[Id(0, Name = "Id", Type = "int")]
        //[Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        //[Property()]
        public virtual string Name { get; set; }
    }
}
