using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "loadflow_boundary_zones")]
    public class BoundaryZone
    {
        public BoundaryZone()
        {

        }

        public BoundaryZone(Boundary b, Zone z)
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
        public virtual Boundary Boundary { get; set; }

        [ManyToOne(Column = "ZoneId", Cascade = "none")]
        public virtual Zone Zone { get; set; }
    }
}