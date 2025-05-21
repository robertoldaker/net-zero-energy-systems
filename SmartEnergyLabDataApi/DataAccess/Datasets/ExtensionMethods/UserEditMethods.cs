namespace SmartEnergyLabDataApi.Data;

public static class UserEditMethods {

    public static double GetDoubleValue(this UserEdit ue) {
        double val;
        double.TryParse(ue.Value, out val);
        return val;
    }

    public static bool GetBoolValue(this UserEdit ue) {
        bool val;
        bool.TryParse(ue.Value, out val);
        return val;
    }
    public static object GetEnumValue(this UserEdit ue, Type type) {
        object val;
        Enum.TryParse(type, ue.Value, out val);
        return val;
    }
}
