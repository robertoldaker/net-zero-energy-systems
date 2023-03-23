namespace SmartEnergyLabDataApi.Data
{
    public static class LinkMethods {

        public static string GetKey(this Link obj) {
            return obj.Name;
        }

    }
}