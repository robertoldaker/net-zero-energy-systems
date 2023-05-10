using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    /// <summary>
    /// Distribution network operators
    /// </summary>
    public enum DNOAreas
    {
        EastEngland,
        EastMidlands,
        London,
        NorthEastEngland,
        NorthScotland,
        NorthWales,
        NorthWestEngland,
        SouthEastEngland,
        SouthernEngland,
        SouthScotland,
        SouthWales,
        SouthWestEngland,
        WestMidlands,
        Yorkshire
    }

    public class DefaultArea {
        public readonly static DefaultArea[] Values = {
            new DefaultArea(DNOAreas.EastEngland,DNOCode.UKPowerNetworks,"East England"),
            new DefaultArea(DNOAreas.EastMidlands,DNOCode.NationalGridElectricityDistribution,"East Midlands"),
            new DefaultArea(DNOAreas.London,DNOCode.UKPowerNetworks,"London"),
            new DefaultArea(DNOAreas.NorthEastEngland,DNOCode.NorthernPowerGrid,"North East England"),
            new DefaultArea(DNOAreas.NorthScotland,DNOCode.ScottishAndSouthernElectricityNetworks,"North Scotland"),
            new DefaultArea(DNOAreas.NorthWales,DNOCode.SPEnergyNetworks,"North Wales"),
            new DefaultArea(DNOAreas.NorthWestEngland,DNOCode.ElectricityNorthWest,"North West England"),
            new DefaultArea(DNOAreas.SouthEastEngland,DNOCode.UKPowerNetworks,"South East England"),
            new DefaultArea(DNOAreas.SouthernEngland,DNOCode.ScottishAndSouthernElectricityNetworks,"Southern England"),
            new DefaultArea(DNOAreas.SouthScotland,DNOCode.SPEnergyNetworks,"South Scotland"),
            new DefaultArea(DNOAreas.SouthWales,DNOCode.NationalGridElectricityDistribution,"South Wales"),
            new DefaultArea(DNOAreas.SouthWestEngland,DNOCode.NationalGridElectricityDistribution,"South West England"),
            new DefaultArea(DNOAreas.WestMidlands,DNOCode.NationalGridElectricityDistribution,"West Midlands"),
            new DefaultArea(DNOAreas.Yorkshire,DNOCode.NorthernPowerGrid,"Yorksire"),
        };
        public DefaultArea(DNOAreas area, DNOCode code, string name) {
            Code = code;
            Area = area;
            Name = name;
        }
        public string Name {get; set;}
        public DNOAreas Area {get;set;}
        public DNOCode Code {get; set;}
    }

    [Class(0, Table = "geographical_areas")]
    public class GeographicalArea
    {
        public GeographicalArea()
        {

        }

        public GeographicalArea(DNOAreas area, string name, DistributionNetworkOperator dno)
        {
            Name = name;
            DistributionNetworkOperator = dno;
            DNOArea = area;
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
        public virtual DNOAreas DNOArea { get; set; }

        [Property(0)]
        [Formula(1, Content = "( select count(*) from grid_supply_points ds where (ds.geographicalareaid = id) )")] 
        public virtual int NumberOfGridSupplyPoints {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan")]
        public virtual GISData GISData { get; set; }
        

        /// <summary>
        /// Distribtion network operator
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "DistributionNetworkOperatorId", Cascade = "none")]
        public virtual DistributionNetworkOperator DistributionNetworkOperator { get; set; }

    }
}
