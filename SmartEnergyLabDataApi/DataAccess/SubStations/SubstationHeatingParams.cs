using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "substation_heating_params")]
    public class SubstationHeatingParams
    {
        public SubstationHeatingParams()
        {

        }
        public SubstationHeatingParams(DistributionSubstation distributionSubstation)
        {
            DistributionSubstation = distributionSubstation;
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }


        [Property()]
        public virtual int NumType1HPs {get; set;}

        [Property()]
        public virtual int NumType2HPs {get; set;}

        [Property()]
        public virtual int NumType3HPs {get; set;}


        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }
    }
}

