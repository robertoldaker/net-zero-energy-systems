using NHibernate.Classic;
using NHibernate.Mapping.Attributes;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Data
{

    public enum ImportSource {
        File,
        NationalGridDistributionOpenData,
        UKPowerNetworksOpenData,
        NorthernPowerGridOpenData,
        ScottishAndSouthernOpenData
    }

    [Class(0, Table = "grid_supply_points")]
    public class GridSupplyPoint : ILifecycle
    {
        public GridSupplyPoint()
        {

        }

        public GridSupplyPoint(ImportSource source, string name, string externalId, string externalId2, GeographicalArea ga, DistributionNetworkOperator dno)
        {
            Source = source;
            Name = name;
            ExternalId = externalId;
            ExternalId2 = externalId2;
            DistributionNetworkOperator = dno;
            GeographicalArea = ga;
            GISData = new GISData();
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual ImportSource Source {get; set;}

        [Property()]
        public virtual string NR { get; set; }

        [Property()]
        public virtual string NRId { get; set; }

        [Property()]
        public virtual string ExternalId { get; set; }

        [Property()]
        public virtual string ExternalId2 { get; set; }

        [Property()]
        public virtual string Name { get; set; }

        [Property(NotNull = true)]
        [Column( Name = "IsDummy", Default ="false")]
        public virtual bool IsDummy {get; set;}

        [Property(NotNull = true)]
        [Column( Name = "NeedsNudge", Default ="false")]
        public virtual bool NeedsNudge {get; set;}

        [Property(0)]
        [Formula(1, Content = "( select count(*) from solar_installations si where (si.gridsupplypointid = id))")]
        public virtual bool NumberOfSolarInstallations {get; set;}

        [Property(0)]
        [Formula(1, Content = "( select count(*) from primary_substations ds where (ds.gridsupplypointid = id) )")]
        public virtual int NumberOfPrimarySubstations {get; set;}

        /// <summary>
        /// GIS data
        /// </summary>
        [ManyToOne(Column = "GISDataId", Cascade = "all-delete-orphan")]
        public virtual GISData GISData { get; set; }

        public virtual DNOAreas DNOArea
        {
            get {
                return GeographicalArea.DNOArea;
            }
        }

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

            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

    }
}
