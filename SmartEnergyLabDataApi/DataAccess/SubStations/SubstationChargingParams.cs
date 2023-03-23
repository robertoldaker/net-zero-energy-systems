using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "substation_charging_params")]
    public class SubstationChargingParams
    {
        public SubstationChargingParams()
        {

        }
        public SubstationChargingParams(DistributionSubstation distributionSubstation)
        {
            DistributionSubstation = distributionSubstation;
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual int NumHomeChargers {get; set;}

        [Property()]
        public virtual int NumType1EVs {get; set;}

        [Property()]
        public virtual int NumType2EVs {get; set;}

        [Property()]
        public virtual int NumType3EVs {get; set;}


        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }
    }
}

