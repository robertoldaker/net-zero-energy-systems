using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{

    [Class(0, Table = "gis_boundaries")]
    public class GISBoundary
    {
        public GISBoundary()
        {

        }

        public GISBoundary(GISData gisData)
        {
            GISData = gisData;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "double precision[]", Name = "Latitudes")]
        public virtual double[] Latitudes { get; set; }

        [Property(Type="HaloSoft.DataAccess.DoubleArrayType, DataAccessBase")]
        [Column(SqlType = "double precision[]", Name = "Longitudes")]
        public virtual double[] Longitudes { get; set; }

        [JsonIgnore]
        [ManyToOne(Column = "GISDataId", Cascade = "none")]
        public virtual GISData GISData { get; set; }

    }
}