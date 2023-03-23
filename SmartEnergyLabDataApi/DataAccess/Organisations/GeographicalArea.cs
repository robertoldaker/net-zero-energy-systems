using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "geographical_areas")]
    public class GeographicalArea
    {
        public GeographicalArea()
        {

        }

        public GeographicalArea(string name,DistributionNetworkOperator dno)
        {
            Name = name;
            DistributionNetworkOperator = dno;
        }
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name { get; set; }

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
