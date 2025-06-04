using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [ApplicationGroup(ApplicationGroup.Elsi)]
    [Class(0, Table = "elsi_misc_params")]
    public class MiscParams : IDatasetIId
    {
        public MiscParams()
        {

        }

        public MiscParams(Dataset dataset) {
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual double EU_CO2 { get; set; }

        [Property()]
        public virtual double GB_CO2 { get; set; }

        [Property()]
        public virtual double VLL { get; set; }

        [Property()]
        public virtual double GBPConv {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }


    }
}
