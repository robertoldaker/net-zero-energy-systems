using SmartEnergyLabDataApi.Data;

public static class GridSupplyPointMethods {
    public static int GetBoundaryLength(this GridSupplyPoint gsp) {
        if ( gsp.GISData!=null && gsp.GISData.BoundaryLatitudes!=null) {
            return gsp.GISData.BoundaryLatitudes.Length;
        } else {
            return 0;
        }
    }
}