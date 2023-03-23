namespace SmartEnergyLabDataApi.Data
{
    public static class ElsiGenDataTypeMethods {
        public static string GetName(this ElsiGenType gt) {
            if ( gt == ElsiGenType.HardCoal) {
                return "Hard coal";
            } else if ( gt == ElsiGenType.HydroPump) {
                return "Hydro-pump";
            } else if ( gt == ElsiGenType.HydroRun) {
                return "Hydro-run";
            } else if ( gt == ElsiGenType.HydroTurbine) {
                return "Hyfro-turbine";
            } else if ( gt == ElsiGenType.OtherNonRes) {
                return "Other non-RES";
            } else if ( gt == ElsiGenType.OtherRes) {
                return "Other RES";
            } else if ( gt == ElsiGenType.SolarPv) {
                return "Solar-PV";
            } else if ( gt == ElsiGenType.WindOffShore) {
                return "Wind-off-shore";
            } else if ( gt == ElsiGenType.WindOnShore) {
                return "Wind-on-shore";
            } else {
                return gt.ToString();
            }
        }
    }
}