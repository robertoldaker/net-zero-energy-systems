using System.Text.Json.Serialization;
using NHibernate.Linq.Functions;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    [ApplicationGroup(ApplicationGroup.BoundCalc)]
    [Class(0, Table = "boundcalc_nodes")]
    public class Node : IId, IDataset
    {
        public Node()
        {

        }

        public Node(Dataset dataset)
        {
            this.Dataset = dataset;
        }
        
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Code {get; set;}

        [Property()]
        public virtual double Demand {get; set;}

        [Property()]
        public virtual double Generation_A {get; set;}

        [Property()]
        public virtual double Generation_B {get; set;}

        public virtual double GetGeneration(TransportModelOld model) {
            if ( model == TransportModelOld.PeakSecurity) {
                return Generation_A;
            } else if ( model == TransportModelOld.YearRound) {
                return Generation_B;
            } else {
                throw new Exception($"Unexpected transport model found [{model}]");
            }
        }

        [Property()]
        public virtual int? Gen_Zone {get; set;}

        [Property()]
        public virtual int? Dem_zone {get; set;}

        [Property()]
        public virtual bool Ext {get; set;}

        [Property()]
        public virtual int Voltage {get; set;}
        

        [ManyToOne(Column = "ZoneId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual Zone Zone {get; set;}
        

        /// <summary>
        /// location of node
        /// </summary>
        [ManyToOne(Column = "locationId", Cascade = "none", Fetch = FetchMode.Join)]
        public virtual GridSubstationLocation Location { get; set; }

        public virtual string Name { 
            get {
                if ( this.Location!=null ) {
                    return Location.Name;
                } else {
                    return null;
                }
            }
        }
        
        [JsonIgnore()]
        [ManyToOne(Column = "DatasetId", Cascade = "none")]
        public virtual Dataset Dataset { get; set; }

        public virtual int DatasetId {
            get {
                return this.Dataset.Id;
            }
        }    
        
        [ManyToOne(Column = "GeneratorId", Cascade = "none")]
        public virtual Generator Generator { get; set; }
        
        public virtual string ZoneName
        {
            get
            {
                return Zone?.Code;
            }
        }

        public virtual double? Mismatch {get; set;}    

        public virtual double? TLF {get; set;}    

        public virtual double? km {get; set;}    

    }
}
