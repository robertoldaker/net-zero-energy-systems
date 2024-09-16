using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    public enum GridSubstationSource { NGET, SHET, SPT}

    [Class(0, Table = "grid_substations")]
    public class GridSubstation
    {
        public GridSubstation()
        {

        }

        public static GridSubstation Create(string reference, GridSubstationSource source) {
            var gs = new GridSubstation();
            gs.GISData = new GISData();
            gs.Reference = reference;
            gs.Source = source;
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
        /// Source of GridSubstation
        /// </summary>
        /// <value></value>
        [Property()]
        [Column(Name = "source",Default = "0")]
        public virtual GridSubstationSource Source {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        public static GridSubstationSource? getSource(GridSubstationLocationSource locSource) {
            if ( locSource == GridSubstationLocationSource.NGET) {
                return GridSubstationSource.NGET;
            } else if ( locSource == GridSubstationLocationSource.SHET) {
                return GridSubstationSource.SHET;
            } else if ( locSource == GridSubstationLocationSource.SPT) {
                return GridSubstationSource.SPT;
            } else {
                return null;
            }
        }

    }
}