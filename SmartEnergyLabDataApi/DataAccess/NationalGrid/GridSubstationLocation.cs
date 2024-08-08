using Google.Apis.Sheets.v4.Data;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    public enum GridSubstationLocationSource { NGET, SHET, SPT, GoogleMaps, Estimated, UserDefined}
    
    [Class(0, Table = "grid_substation_locations")]
    public class GridSubstationLocation : IId, IDataset, ILifecycle
    {
        public GridSubstationLocation()
        {

        }

        public static GridSubstationLocation Create(string reference, GridSubstationLocationSource source, Dataset dataset) {
            var gs = new GridSubstationLocation();
            gs.GISData = new GISData();
            gs.Reference = reference;
            gs.Source = source;
            gs.Dataset = dataset;
            return gs;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]        
        public virtual int Id { get; set; }

        /// <summary>
        /// Name of grid substation
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Reference for grid substation
        /// </summary>
        [Property()]
        public virtual string Reference { get; set; } 

        /// <summary>
        /// Source of location
        /// </summary>
        /// <value></value>
        [Property()]
        [Column(Name = "source",Default = "0")]
        public virtual GridSubstationLocationSource Source {get; set;}
        
        /// <summary>
        /// Latitude
        /// </summary>
        public virtual double Latitude { 
            get {
                return GISData.Latitude;
            }
            set {
                GISData.Latitude = value;
            }
        }

        /// <summary>
        /// Longitude
        /// </summary>
        public virtual double Longitude { 
            get {
                return GISData.Longitude;
            }
            set {
                GISData.Longitude = value;
            }
        }

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId {
            get {
                return this.Dataset.Id;
            }
        }    
        
        public virtual LifecycleVeto OnSave(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnUpdate(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

        public virtual GridSubstationLocation Copy(Dataset ds) {
            var loc = new GridSubstationLocation();
            loc.Name = this.Name;
            loc.Reference = this.Reference;
            loc.Source = this.Source;
            loc.GISData = new GISData();
            loc.GISData.Latitude = this.GISData.Latitude;
            loc.GISData.Longitude = this.GISData.Longitude;
            loc.Dataset = ds;
            //
            return loc;
        }

    }
}
