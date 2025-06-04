using System.Text.Json.Serialization;
using NHibernate.Classic;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_zones")]
    public class Zone : IDatasetIId, ILifecycle
    {
        public Zone()
        {
            TGeneration = 0;
            Tdemand = 0;
            UnscaleDem = 0;
            UnscaleGen = 0;
        }

        public Zone(Dataset dataset)
        {
            Dataset = dataset;
        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Code {get; set;}

        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId {
            get {
                return Dataset.Id;
            }
        }

        public virtual double TGeneration {get; set;}

        public virtual double Tdemand {get; set;}

        public virtual double UnscaleGen {get; set;}

        public virtual double UnscaleDem {get; set;}

        public virtual LifecycleVeto OnDelete(NHibernate.ISession s)
        {
            //
            var nds = s.QueryOver<Node>().Where( m=>m.Zone == this).List();
            foreach( var nd in nds) {
                nd.Zone = null;
            }
            //
            var bzs = s.QueryOver<BoundaryZone>().Where( m=>m.Zone == this).List();
            foreach( var bz in bzs) {
                s.Delete(bz);
            }
            return LifecycleVeto.NoVeto;
        }

        public virtual void OnLoad(NHibernate.ISession s, object id)
        {
        }

        public virtual LifecycleVeto OnSave(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }

        public virtual LifecycleVeto OnUpdate(NHibernate.ISession s)
        {
            return LifecycleVeto.NoVeto;
        }
    }
}
