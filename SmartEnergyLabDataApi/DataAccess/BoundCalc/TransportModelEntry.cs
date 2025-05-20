using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_transport_model_entries")]
    public class TransportModelEntry : IId
    {
        public TransportModelEntry()
        {

        }

        public TransportModelEntry(TransportModel tm)
        {
            this.TransportModel = tm;
        }
        
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual GeneratorType GeneratorType {get; set;}

        [Property()]
        public virtual bool AutoScaling {get; set;}

        [Property()]
        public virtual double Scaling {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "TransportModelId", Cascade = "none")]
        public virtual TransportModel TransportModel { get; set; }
        
    }
}
