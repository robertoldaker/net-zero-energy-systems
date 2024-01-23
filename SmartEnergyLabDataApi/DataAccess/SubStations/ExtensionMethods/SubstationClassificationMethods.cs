using NHibernate;

namespace SmartEnergyLabDataApi.Data
{
    public static class SubstationClassificationMethods
    {
        public static List<SubstationClassification> Aggregate(this IList<SubstationClassification> classifications)
        {
            var sscDict = new Dictionary<int, SubstationClassification>();
            foreach (var cl in classifications)
            {
                var key = cl.Num;
                if (!sscDict.ContainsKey(key))
                {
                    sscDict.Add(key, cl);
                }
                else
                {
                    var sum = sscDict[key];
                    sum.NumberOfEACs += cl.NumberOfEACs;
                    sum.NumberOfCustomers += cl.NumberOfCustomers;
                    sum.ConsumptionKwh += cl.ConsumptionKwh;
                }
            }
            //
            return sscDict.Values.ToList();
        }
        
        public static int[] GetElexonProfile(this IList<SubstationClassification> classifications)
        {
            int[] data = new int[8];
            for (int i = 0; i < 8; i++)
            {
                // Spreadsheet from Bath had number of EACS but later spreadsheet for areas in Wales only had number of customers
                int value = classifications.Where(m => m.Num == i + 1).Select(m => m.NumberOfEACs!=0 ? m.NumberOfEACs : m.NumberOfCustomers).FirstOrDefault();
                data[i] = value;
            }
            return data;
        }
    }
}