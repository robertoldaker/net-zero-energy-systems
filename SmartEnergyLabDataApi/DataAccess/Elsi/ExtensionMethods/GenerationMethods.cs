namespace SmartEnergyLabDataApi.Data
{
    public static class GenerationMethods {
        public static string GetKey(int day, ElsiProfile profile, ElsiPeriod period, ElsiGenDataType genType) {
            return $"{day}:{profile}:{period}:{genType}";
        }

        public static string GetKey(this AvailOrDemand obj) {
            return GenerationMethods.GetKey(obj.Day,obj.Profile,obj.Period,obj.DataType);
        }
    }
}