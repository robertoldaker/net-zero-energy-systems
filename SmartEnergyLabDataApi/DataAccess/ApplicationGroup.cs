using System.Reflection;
using Microsoft.Extensions.ObjectPool;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data;

[Flags]
public enum ApplicationGroup { All, Loadflow, Elsi }

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ApplicationGroupAttribute : Attribute {
    public ApplicationGroupAttribute(params ApplicationGroup[] groups) {
        Groups = groups;
    }

    public ApplicationGroup[] Groups {get; set;}

    public static List<string> GetClassNames(ApplicationGroup appGroup) {

        // Get the current assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Get all types in the assembly
        Type[] types = assembly.GetTypes();

        var classNames = new List<string>();

        foreach (Type type in types)
        {
            // Get all attributes of the type
            var appGroupAttr = type.GetCustomAttributes<ApplicationGroupAttribute>().FirstOrDefault();
            if ( appGroupAttr!=null && appGroupAttr.HasGroup(appGroup) ) {
                var classAttr = type.GetCustomAttributes<ClassAttribute>().FirstOrDefault();
                classNames.Add(classAttr.Table);
            }
        }

        //
        return classNames;
    }

    private bool HasGroup(ApplicationGroup appGroup) {
        return Groups.Where(m=>appGroup.HasFlag(m)).Any();
    }
}


