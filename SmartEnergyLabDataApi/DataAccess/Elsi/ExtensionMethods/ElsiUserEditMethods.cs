namespace SmartEnergyLabDataApi.Data
{
    public static class ElsiUserEditMethods {

        public static double GetDoubleValue(this ElsiUserEdit ue) {
            double val;
            double.TryParse(ue.Value, out val);
            return val;
        }

        public static bool GetBoolValue(this ElsiUserEdit ue) {
            bool val;
            bool.TryParse(ue.Value, out val);
            return val;
        }
    }
}