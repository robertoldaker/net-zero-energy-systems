using SmartEnergyLabDataApi.Data;

public static class GridSupplyPointMethods {
    public static int GetMaxBoundaryLength(this GridSupplyPoint gsp, DataAccess da) {
        if ( gsp.GISData!=null) {
            return gsp.GISData.GetMaxBoundaryLength(da);
        } else {
            return 0;
        }
    }
}