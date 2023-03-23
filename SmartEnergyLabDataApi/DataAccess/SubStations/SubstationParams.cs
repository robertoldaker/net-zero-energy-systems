using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data 
{
    public enum SubstationMount {Ground,Pole}

    [Class(0, Table = "substation_params")]
    public class SubstationParams
    {
        public SubstationParams()
        {

        }

        public SubstationParams(DistributionSubstation dss)
        {
            DistributionSubstation = dss;
            // Defaults taken from the classification tool spreadsheet
            Mount = SubstationMount.Ground;
            Rating = 500;
            PercentIndustrialCustomers = 16;
            NumberOfFeeders = 3;
            PercentageHalfHourlyLoad = 0;
            TotalLength = 1.404;
            PercentageOverhead = 0;
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual SubstationMount Mount { get; set;}

        [Property()]
        public virtual double Rating {get; set;}

        [Property()]
        public virtual int PercentIndustrialCustomers {get; set;}

        [Property()]
        public virtual int NumberOfFeeders {get; set;}

        [Property()]
        public virtual int PercentageHalfHourlyLoad {get; set;}

        [Property()]
        public virtual double TotalLength {get; set;}

        [Property()]
        public virtual int PercentageOverhead {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation? DistributionSubstation { get; set; }

    }

}