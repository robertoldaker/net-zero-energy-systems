using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    public static class BranchMethods {
        public static string GetKey(this Branch b) {
            return b.LineName;
        }
        public static void SetCode(this Branch b, string key) {
            var cpnts = key.Split(':');
            b.Code = cpnts[1];
        }
    }
}