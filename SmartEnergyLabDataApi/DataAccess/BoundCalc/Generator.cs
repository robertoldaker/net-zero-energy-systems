using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public enum GeneratorType
    {
        Biomass,
        CCGT,
        CHP,
        Coal,
        Hydro,
        Interconnector,
        Nuclear,
        OCGT,
        PumpStorage,
        Tidal,
        WindOffshore,
        WindOnshore
    }
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_generators")]
    public class Generator : IId, IDataset
    {
        public Generator()
        {

        }

        public Generator(Dataset dataset)
        {
            this.Dataset = dataset;
        }
        
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name {get; set;}

        [Property()]
        public virtual double Capacity {get; set;}

        [Property()]
        public virtual GeneratorType Type {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId {
            get {
                return this.Dataset.Id;
            }
        }    
        
    }
}
