using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_transport_models")]
    public class TransportModel : IId, IDataset
    {
        public TransportModel()
        {
            Entries = new List<TransportModelEntry>();
        }

        public TransportModel(Dataset dataset)
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
        public virtual string Name { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId
        {
            get
            {
                return this.Dataset.Id;
            }
        }

        [JsonIgnore()]
        public virtual IList<TransportModelEntry> Entries { get; set; }

    }
}
