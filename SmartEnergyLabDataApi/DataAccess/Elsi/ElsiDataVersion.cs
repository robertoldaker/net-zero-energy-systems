using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartEnergyLabDataApi.Data
{    
    [ValidateNever] // required as using this class in model binding and without it will get errors for any missing fields
    [Class(0, Table = "elsi_data_version")]
    public class ElsiDataVersion
    {
        public ElsiDataVersion()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name {get; set;}

        [ManyToOne(Column = "ParentId", Cascade = "none")]
        public virtual ElsiDataVersion Parent {get; set;}

        [JsonIgnore]
        [ManyToOne(Column = "UserId", Cascade = "none")]
        public virtual User User {get; set;}

    }
}