using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "grid_supply_points")]
    public class GridSupplyPoint
    {
        public GridSupplyPoint()
        {

        }

        public GridSupplyPoint(string name, string nr, string nrId, GeographicalArea ga, DistributionNetworkOperator dno)
        {
            Name = name;
            NR = nr;
            NRId = nrId;
            DistributionNetworkOperator = dno;
            GeographicalArea = ga;
            GISData = new GISData();
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string NR { get; set; }

        [Property()]
        public virtual string NRId { get; set; }

        [Property()]
        public virtual string Name { get; set; }

        [Property(0)]
        [Formula(1, Content = "( select count(*) from primary_substations ds where (ds.gridsupplypointid = id) )")] 
        public virtual int NumberOfPrimarySubstations {get; set;}

                /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan")]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Geographical area
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }

        /// <summary>
        /// Distribtion network operator
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "DistributionNetworkOperatorId", Cascade = "none")]
        public virtual DistributionNetworkOperator DistributionNetworkOperator { get; set; }


    }
}