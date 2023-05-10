using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{    
    public enum ImportState { OK, Error}

    [Class(0, Table = "data_source_info")]
    public class DataSourceInfo
    {
        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name {get; set;}

        [Property()]
        public virtual string Url {get; set;}

        [Property()]
        public virtual string Reference {get; set;}

        [Property()]
        public virtual DateTime? LastModified {get; set;}
                
        [Property()]
        public virtual DateTime? LastImported {get; set;}

        [Property()]
        public virtual ImportState State {get; set;}

        [Property()]
        public virtual string Message {get; set;}



    }
}