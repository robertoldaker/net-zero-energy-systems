using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_transport_model_entries")]
    public class GenerationModelEntry : IDatasetIId
    {
        public GenerationModelEntry()
        {

        }

        public GenerationModelEntry(GenerationModel tm, Dataset dataset)
        {
            this.GenerationModel = tm;
            this.Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        [Property()]
        public virtual GeneratorType GeneratorType { get; set; }

        [Property()]
        public virtual bool AutoScaling { get; set; }

        [Property()]
        public virtual double Scaling { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "TransportModelId", Cascade = "none")]
        public virtual GenerationModel GenerationModel { get; set; }

        public virtual int GenerationModelId
        {
            get
            {
                return GenerationModel.Id;
            }
        }

        public virtual double TotalCapacity { get; set; }
    }
}
