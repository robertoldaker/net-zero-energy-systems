using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "grid_overhead_lines")]
    public class GridOverheadLine
    {
        public GridOverheadLine()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]        
        public virtual int Id { get; set; }

        /// <summary>
        /// Reference for grid overhead line
        /// </summary>
        [Property()]
        public virtual string Reference { get; set; }

        /// <summary>
        /// Name of grid substation
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Circuit 1
        /// </summary>
        [Property()]
        public virtual string Circuit1 { get; set; }

        /// <summary>
        /// Circuit 2
        /// </summary>
        [Property()]
        public virtual string Circuit2 { get; set; }

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

    }
}