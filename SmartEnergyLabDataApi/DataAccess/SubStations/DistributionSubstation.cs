using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{
    [Class(0, Table = "distribution_substations")]
    public class DistributionSubstation : ILifecycle
    {
        public DistributionSubstation()
        {

        }

        public DistributionSubstation(ImportSource source, string externalId, string externalId2, PrimarySubstation primarySubstation) {
            Source = source;
            ExternalId = externalId;
            ExternalId2 = externalId2;
            initialise(primarySubstation);
        }

        private void initialise(PrimarySubstation primarySubstation) {
            PrimarySubstation = primarySubstation;
            GridSupplyPoint = primarySubstation.GridSupplyPoint;
            GeographicalArea = primarySubstation.GeographicalArea;
            GISData = new GISData();
            SubstationParams = new SubstationParams(this);
            SubstationData = new DistributionSubstationData(this);
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]        
        public virtual int Id { get; set; }

        /// <summary>
        /// Source from where the object was imported
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual ImportSource Source {get; set;}

        /// <summary>
        /// External reference to object
        /// </summary>
        [Property()]
        public virtual string ExternalId { get; set; }

        /// <summary>
        /// Extra reference used by some importers
        /// </summary>
        [Property()]
        public virtual string ExternalId2 { get; set; }

        /// <summary>
        /// Reference used by National Grid Distribution
        /// </summary>
        [Property()]
        public virtual string NR { get; set; }

        /// <summary>
        /// Another reference used by National Grid Distribution
        /// </summary>
        /// <value></value>
        [Property()]
        public virtual string NRId { get; set; }

        /// <summary>
        /// Name of distribution substation
        /// </summary>
        [Property()]
        public virtual string Name { get; set; }

        /// <summary>
        /// Primary substation it is connected to
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "PrimarySubstationId", Cascade = "none")]
        public virtual PrimarySubstation PrimarySubstation { get; set; }

        /// <summary>
        /// Grid supply point its connected to
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GridSupplyPointId", Cascade = "none")]
        public virtual GridSupplyPoint GridSupplyPoint { get; set; }

        /// <summary>
        /// Geographical area it is in
        /// </summary>
        [JsonIgnore]
        [ManyToOne(Column = "GeographicalAreaId", Cascade = "none")]
        public virtual GeographicalArea GeographicalArea { get; set; }

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual GISData GISData { get; set; }

        /// <summary>
        /// Charging parameters
        /// </summary>
        [ManyToOne(Column = "ChargingParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationChargingParams ChargingParams { get; set; }

        /// <summary>
        /// Heating parameters
        /// </summary>
        [ManyToOne(Column = "HeatingParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationHeatingParams HeatingParams { get; set; }

        /// <summary>
        /// Substation parameters
        /// </summary>
        [ManyToOne(Column = "SubstationParamsId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual SubstationParams SubstationParams { get; set; }

        /// <summary>
        /// Substation data
        /// </summary>
        [ManyToOne(Column = "DistributionSubstationDataId", Cascade = "all-delete-orphan", Fetch = FetchMode.Join)]
        public virtual DistributionSubstationData SubstationData { get; set; }

        public virtual IList<SubstationLoadProfile> LoadProfiles { get; set; }
        public virtual void LoadLoadProfiles(DataAccess da)
        {
            LoadProfiles = da.SubstationLoadProfiles.GetSubstationLoadProfiles(this);
        }

        public virtual IList<SubstationClassification> Classifications { get; set; }

        public virtual void LoadClassifications(DataAccess da)
        {
            Classifications = da.SubstationClassifications.GetSubstationClassifications(this);
        }

        public virtual LifecycleVeto OnSave(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnUpdate(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual void Unlink() {
            if ( this.SubstationParams!=null) {
                this.SubstationParams.DistributionSubstation = null;
            }
            if ( this.SubstationData!=null) {
                this.SubstationData.DistributionSubstation = null;
            }
            if ( this.HeatingParams!=null ) {
                this.HeatingParams.DistributionSubstation = null;
            }
            if ( this.ChargingParams!=null) {
                this.ChargingParams.DistributionSubstation = null;
            }
            //

            
        }

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            // Delete substation classifications
            var dscs = s.QueryOver<SubstationClassification>().Where(m=>m.DistributionSubstation.Id==this.Id).List();
            foreach( var dsc in dscs) {
                s.Delete(dsc);
            }
            // Delete substation load profiles
            var slps = s.QueryOver<SubstationLoadProfile>().Where(m=>m.DistributionSubstation.Id==this.Id).List();
            foreach( var slp in slps) {
                // Don't delete those for from EV and HP prediction since not sure how these can be reloaded
                if ( slp.Source == LoadProfileSource.LV_Spreadsheet || slp.Source == LoadProfileSource.Tool) {
                    s.Delete(slp);
                }
            }
            //?? This should be able to removed as there should only be one and it should get removed with unlink
            var dsps = s.QueryOver<SubstationParams>().Where(m=>m.DistributionSubstation.Id==this.Id).List();
            foreach( var dsp in dsps) {
                dsp.DistributionSubstation = null;
            }
            //
            this.Unlink();
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }
    }
}
