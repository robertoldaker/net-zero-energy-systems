using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartEnergyLabDataApi.Data
{    
    [ValidateNever] // required as using this class in model binding and without it will get errors for any missing fields
    [Class(0, Table = "elsi_user_edit")]
    public class ElsiUserEdit
    {
        public ElsiUserEdit()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Key {get; set;}

        [Property()]
        public virtual string TableName {get; set;}

        [Property()]
        public virtual string ColumnName {get; set;}

        [Property()]
        public virtual string Value {get; set;}

        public virtual int VersionId {get; set; }

        [ManyToOne(Column = "VersionId", Cascade = "none")]
        public virtual ElsiDataVersion Version {get; set;}


    }
}