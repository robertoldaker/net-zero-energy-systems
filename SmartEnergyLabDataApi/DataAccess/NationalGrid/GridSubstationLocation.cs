using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    public enum GridSubstationLocationSource { NGET, SHET, SPT, GoogleMaps, Estimated}
    
    [Class(0, Table = "grid_substation_locations")]
    public class GridSubstationLocation : ILifecycle
    {
        public GridSubstationLocation()
        {

        }

        public static GridSubstationLocation Create(string reference, GridSubstationLocationSource source) {
            var gs = new GridSubstationLocation();
            gs.GISData = new GISData();
            gs.Reference = reference;
            gs.Source = source;
            return gs;
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
            var nodes = s.QueryOver<Node>().Where( m=>m.Location == this).List();
            foreach( var n in nodes) {
                n.Location = null;
            }
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
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
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }


    }
}
