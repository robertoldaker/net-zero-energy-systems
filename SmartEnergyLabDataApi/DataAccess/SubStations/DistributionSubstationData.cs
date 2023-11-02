using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data 
{
    public enum DistributionSubstationType {Ground,Pole}

    [Class(0, Table = "distribution_substation_data")]
    public class DistributionSubstationData
    {
        public DistributionSubstationData()
        {
        }

        public DistributionSubstationData(DistributionSubstation dss)
        {
            DistributionSubstation = dss;
            dss.SubstationData=this;
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual DistributionSubstationType Type { get; set;}

        [Property()]
        public virtual string HVFeeder {get; set;}

        [Property()]
        public virtual double DayMaxDemand {get; set;}

        [Property()]
        public virtual double NightMaxDemand {get; set;}

        [Property()]
        public virtual double Rating {get; set;}

        [Property()]
        public virtual int NumEnergyStorage {get; set;}

        [Property()]
        public virtual int NumHeatPumps {get; set;}
    
        [Property()]
        public virtual int NumEVChargers {get; set;}

        [Property()]
        public virtual double TotalLCTCapacity {get;set;}

        [Property()]
        public virtual double TotalGenerationCapacity {get; set;}

        [Property()]
        public virtual int NumCustomers {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }

    }

}

