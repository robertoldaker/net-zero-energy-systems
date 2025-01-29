using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_boundary_zones")]
    public class BoundCalcBoundaryZone : IId, IDataset
    {
        public BoundCalcBoundaryZone()
        {

        }

        public BoundCalcBoundaryZone(BoundCalcBoundary b, BoundCalcZone z)
        {
            Boundary = b;
            Zone = z;
        }
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [ManyToOne(Column = "BoundaryId", Cascade = "none")]
        public virtual BoundCalcBoundary Boundary { get; set; }

        [ManyToOne(Column = "ZoneId", Cascade = "none")]
        public virtual BoundCalcZone Zone { get; set; }
        
        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

    }
}