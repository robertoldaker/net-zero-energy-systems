using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "grid_substations")]
    public class GridSubstation
    {
        public GridSubstation()
        {

        }

        public static GridSubstation Create(string reference) {
            var gs = new GridSubstation();
            gs.GISData = new GISData();
            gs.Reference = reference;
            return gs;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]        
        public virtual int Id { get; set; }

        /// <summary>
        /// Reference for grid substation
        /// </summary>
        [Property()]
        public virtual string Reference { get; set; }

        /// <summary>
        /// Name of grid substation
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Voltage
        /// </summary>
        [Property()]
        public virtual string Voltage { get; set; }

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Loadflow node
        /// </summary>
        [ManyToOne(Column = "LoadflowNodeId", Cascade = "none")]
        public virtual Node LoadflowNode { get; set; }
    }
}