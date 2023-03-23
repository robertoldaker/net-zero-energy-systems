using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    /// <summary>
    /// Distrubtion network operators
    /// </summary>
    public enum DNOCodes
    {
        EasternPowerNetworks,
        ElectricityNorthWest,
        LondonPowerNetworks,
        NorthernPowergridNorthEast,
        NorthernPowergridYorkshire,
        ScottishHydroElectricPowerDistribution,
        SouthEasternPowerNetworks,
        SouthernElectricPowerDistribution,
        SPDistribution,
        SPManweb,
        WesternPowerDistributionEastMidlands,
        WesternPowerDistributionSouthWales,
        WesternPowerDistributionSouthWest,
        WesternPowerDistributionWestMidlands
    }

    [Class(0, Table = "distribution_network_operators")]
    public class DistributionNetworkOperator
    {
        public DistributionNetworkOperator()
        {

        }

        public DistributionNetworkOperator(DNOCodes code, string name)
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
        public virtual DNOCodes Code { get; set; }
    }
}
