using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HaloSoft.DataAccess
{
    public class NHibernateUtilities
    {
        public static void AddMappings(NHibernate.Cfg.Configuration config, string tableSourceName)
        {
            NHibernate.Mapping.Attributes.HbmSerializer.Default.Validate = true;

            var execAss = Assembly.GetExecutingAssembly();
            var ms = GetAttributesStream(config, execAss, tableSourceName);
            using (ms) {
                //
                config.AddInputStream(ms);
            }

            // add classes 
            var callingAss = Assembly.GetEntryAssembly();
            ms = GetAttributesStream(config, callingAss, tableSourceName);
            using (ms)
            {
                //
                config.AddInputStream(ms);
            }
        }

        private static MemoryStream GetAttributesStream(NHibernate.Cfg.Configuration config, Assembly ass, string tableSourceName)
        {
            MemoryStream s = NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(ass);
            byte[] bytes = s.ToArray();
            string str = Encoding.UTF8.GetString(bytes);

            //Console.WriteLine(str);

            string outputStr = FilterAttributes(ass, str, tableSourceName);

            byte[] newBytes = System.Text.UTF8Encoding.UTF8.GetBytes(outputStr);


            return new MemoryStream(newBytes);
        }

        private static string FilterAttributes(Assembly assembly,string inputStr, string tableSourceName)
        {
            // <class table="Couriers" name="SBT.DataAccess.Courier, DataAccess">
            Regex startClassReg = new Regex(@"^\s*<class table=""(.*)"" name=""(.*),");
            
            // </class>
            Regex endClassReg = new Regex(@"^\s*</class>");
            
            // <property name="PayerName" />
            Regex propertyReg = new Regex(@"^\s*<property(.*)name=""(.*?)""");

            var sb = new StringBuilder();
            using (var sr = new StringReader(inputStr))
            {
                string line;
                while ((line = sr.ReadLine())!=null )
                {
                    var m = startClassReg.Match(line);
                    if (m.Success)
                    {
                        var className = m.Groups[2].Value;
                        //
                        var classType = assembly.GetType(className);
                        //
                        if (classType != null)
                        {
                            var source = GetTableSource(classType);
                            bool export = source == tableSourceName;
                            if (export)
                            {
                                sb.AppendLine(line);
                            }
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (export)
                                {
                                    var mmm = propertyReg.Match(line);
                                    if (mmm.Success)
                                    {
                                        line = ModPropertyEntry(line,classType, mmm.Groups[2].Value);
                                    }
                                    sb.AppendLine(line);
                                }
                                var mm = endClassReg.Match(line);
                                if (mm.Success)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("Cannot find class [{0}] in assembly", className));
                        }
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                return sb.ToString();
            }
        }

        private static string ModPropertyEntry(string line, Type classtype, string propertyName)
        {
            var pi = classtype.GetProperty(propertyName);
            if (pi != null)
            {
                // this ensures that string properties get mapped to "TEXT" instead of the default varchar(255).
                if (pi.PropertyType.Name == "String")
                {
                    line = line.Insert(line.Length - 2, @" length=""2000""");
                }
            }
            return line;
        }

        private static string GetTableSource(Type type)
        {
            var attr = type.GetCustomAttributes( typeof(TableSource), true).FirstOrDefault() as TableSource;
            if (attr != null)
            {
                return attr.Name;
            }
            else
            {
                return null;
            }
        }
    }
}
