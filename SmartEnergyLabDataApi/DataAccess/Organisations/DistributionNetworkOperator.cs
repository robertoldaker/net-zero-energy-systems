using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    public enum DNOCode {
        UKPowerNetworks,
        NationalGridElectricityDistribution,
        SPEnergyNetworks,
        NorthernPowerGrid,
        ElectricityNorthWest,
        ScottishAndSouthernElectricityNetworks,
    }

    public class DefaultDNO {
        public readonly static DefaultDNO[] Values = {
            new DefaultDNO(DNOCode.UKPowerNetworks,"UK Power Networks"),
            new DefaultDNO(DNOCode.NationalGridElectricityDistribution,"National Grid Electricity Distribution"),
            new DefaultDNO(DNOCode.SPEnergyNetworks,"SP Energy Networks"),
            new DefaultDNO(DNOCode.NorthernPowerGrid,"Northern Power Grid"),
            new DefaultDNO(DNOCode.ElectricityNorthWest,"Electricity North West"),
            new DefaultDNO(DNOCode.ScottishAndSouthernElectricityNetworks,"Scottish and Southern Electricity Networks"),
        };
        public DefaultDNO(DNOCode code, string name) {
            Code = code;
            Name = name;
        }
        public string Name {get; set;}
        public DNOCode Code {get; set;}
    }

    [Class(0, Table = "distribution_network_operators")]
    public class DistributionNetworkOperator
    {

        public DistributionNetworkOperator()
        {

        }

        public DistributionNetworkOperator(DNOCode code, string name)
        {
            Code = code;
            Name = name;
        }
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name { get; set; }

        [Property()]
        public virtual DNOCode Code { get; set; }

    }
}
