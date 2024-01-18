using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "primary_substations")]
    public class PrimarySubstation : ILifecycle
    {
        public PrimarySubstation()
        {

        }

        public PrimarySubstation(ImportSource source,string externalId, string externalId2, GridSupplyPoint gsp)
        {
            Source = source;
            ExternalId = externalId;
            ExternalId2 = externalId2;
            SetGSP(gsp);
            GISData = new GISData(this);
        }

        private void SetGSP(GridSupplyPoint gsp) {
            GridSupplyPoint = gsp;
            if ( gsp!=null) {
                DistributionNetworkOperator = gsp.DistributionNetworkOperator;
                GeographicalArea = gsp.GeographicalArea;
            }
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }


        /// <summary>
        /// Id of its parent grid supply point
        /// </summary> <summary>
        /// 
        /// </summary>
        /// <value></value>
        public virtual int? GspId {
            get {
                return GridSupplyPoint?.Id;
            }
        }

        /// <summary>
        /// Source of object import
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ImportSource Source {get; set;}

        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Used to identify object during import
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string ExternalId { get; set; }

        /// <summary>
        /// Used to identify object 
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string ExternalId2 { get; set; }

        /// <summary>
        /// Used by National Grid Distribution as a reference
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string NR { get; set; }

        /// <summary>
        /// Another reference used by National Grid Distribution
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string NRId { get; set; }

        [Property(0)]
        [Formula(1, Content = "( select count(*) from distribution_substations ds where (ds.primarysubstationid = id) )")] 
        public virtual int NumberOfDistributionSubstations {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Grid supply point
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
        public virtual GridSupplyPoint GridSupplyPoint { get; set; }

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
            if ( this.GISData!=null ) {
                this.GISData.PrimarySubstation = null;
            }
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

    }
}
