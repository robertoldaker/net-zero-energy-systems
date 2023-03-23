using System;
using System.Collections.Generic;
using System.Text;

namespace HaloSoft.DataAccess
{
    public enum TableSourceType { System, Data }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TableSource : System.Attribute
    {

        public TableSource(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

}
