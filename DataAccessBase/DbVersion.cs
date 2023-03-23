using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Mapping.Attributes;

namespace HaloSoft.DataAccess
{
    [Class(Table = "DbVersion")]
    public class DbVersion
    {
        public DbVersion()
        {
        }

        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual int SchemaVersion { get; set; }

        [Property()]
        public virtual int ScriptVersion { get; set; }

    }
}
