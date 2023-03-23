namespace SmartEnergyLabDataApi.Data
{
    public static class GenCapacityMethods {
        public static string GetKey(ElsiZone zone, ElsiGenType genType, ElsiScenario scenario) {
            return $"{zone}:{genType}:{scenario}";
        }

        public static string GetKey(this GenCapacity obj) {
            return GenCapacityMethods.GetKey(obj.Zone,obj.GenType,obj.Scenario);
        }

        public static bool IsStorage(this ElsiGenType gt) {
            if ( gt == ElsiGenType.Battery || gt == ElsiGenType.HydroPump)  {
                return true;
            } else {
                return false;
            }
        }

    }
}