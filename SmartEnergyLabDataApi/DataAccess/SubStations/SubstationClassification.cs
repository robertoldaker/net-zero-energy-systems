using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "substation_classifications")]
    public class SubstationClassification
    {
        public SubstationClassification()
        {

        }

        public SubstationClassification(int num, DistributionSubstation dss)
        {
            Num = num;
            DistributionSubstation = dss;
            PrimarySubstation = dss.PrimarySubstation;
            GridSupplyPoint = dss.PrimarySubstation.GridSupplyPoint;
            GeographicalArea = dss.PrimarySubstation.GridSupplyPoint.GeographicalArea;
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual int Num { get; set; }

        [Property()]
        public virtual double ConsumptionKwh { get; set; }

        [Property()]
        public virtual int NumberOfCustomers { get; set; }

        [Property()]
        public virtual int NumberOfEACs { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "DistributionSubstationId", Cascade = "none")]
        public virtual DistributionSubstation DistributionSubstation { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation PrimarySubstation { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
        public virtual GridSupplyPoint GridSupplyPoint { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }
    }
}
