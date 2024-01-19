using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using static CommonInterfaces.Models.EVDemandInput;

namespace SmartEnergyLabDataApi.Models;

public class EVDemandUtils {
    public static EVDemandInput CreateFromDistributionId(int id) {
        var evDi = new EVDemandInput();
        using ( var da = new DataAccess()) {
            var dss = da.Substations.GetDistributionSubstation(id);
            if ( dss==null) {
                throw new Exception($"Could not find substation with id=[{id}]");
            }
            var boundaries = da.GIS.GetBoundaries(dss.GISData.Id);
            //
            if ( boundaries.Count>0 ) {
                var rD = new RegionData(id,RegionType.Dist);
                // just use the one with the larget number of points
                var boundary = boundaries.OrderByDescending(m=>m.Latitudes.Count()).First();
                rD.polygon = new Polygon(boundary.Longitudes,boundary.Latitudes);
                if ( dss.SubstationData!=null ) {
                    rD.numCustomers = dss.SubstationData.NumCustomers;
                } else {
                    throw new Exception($"No substation data defined for dss=[{dss.Name}]");
                }
                evDi.regionData.Add(rD);
            } else {
                throw new Exception($"No boundaries defined for dss=[{dss.Name}]");
            }
        }
        return evDi;
    }

    public static EVDemandInput CreateFromPrimaryId(int id) {
        var evDi = new EVDemandInput();
        using ( var da = new DataAccess()) {
            var pss = da.Substations.GetPrimarySubstation(id);
            if ( pss==null) {
                throw new Exception($"Could not find substation with id=[{id}]");
            }
            var boundaries = da.GIS.GetBoundaries(pss.GISData.Id);
            //
            if ( boundaries.Count>0 ) {
                var dsss = da.Substations.GetDistributionSubstations(id);
                var dssDict=new Dictionary<DistributionSubstation,int>();
                foreach( var boundary in boundaries) {
                    var numCustomers=0;
                    foreach( var dss in dsss ) {
                        var lat = dss.GISData.Latitude;
                        var lng = dss.GISData.Longitude;
                        if( dss.SubstationData!=null && GISUtilities.IsPointInPolygon(lat,lng, boundary.Latitudes,boundary.Longitudes )) {
                            numCustomers += dss.SubstationData.NumCustomers;
                            if ( dssDict.ContainsKey(dss)) {
                                dssDict[dss]+=1;
                            } else {
                                dssDict.Add(dss,1);
                            }
                        }
                    }
                    if ( numCustomers>0 ) {
                        var rD = new RegionData(id,RegionType.GSP);
                        rD.polygon=new Polygon(boundary.Longitudes,boundary.Latitudes);
                        rD.numCustomers = numCustomers;
                        evDi.regionData.Add(rD);
                    } 
                }
                //
                foreach( var dss in dsss) {
                    if ( !dssDict.ContainsKey(dss)) {
                        Logger.Instance.LogInfoEvent($"pss [{dss.Name}],[{dss.Id}] not in any Primary boundary");
                    } else if ( dssDict[dss]>1) {
                        Logger.Instance.LogInfoEvent($"pss [{dss.Name}],[{dss.Id}] in multiple Primary boundaries");
                    }
                }
            } else {
                throw new Exception($"No boundaries defined for pss=[{pss.Name}]");
            }
        }
        return evDi;
    }

    public static EVDemandInput CreateFromGridSupplyPointId(int id) {
        var evDi = new EVDemandInput();
        using ( var da = new DataAccess()) {
            var gsp = da.SupplyPoints.GetGridSupplyPoint(id);
            if ( gsp==null) {
                throw new Exception($"Could not find grid supply point with id=[{id}]");
            }
            var boundaries = da.GIS.GetBoundaries(gsp.GISData.Id);
            //
            if ( boundaries.Count>0 ) {
                var psss=da.Substations.GetPrimarySubstationsByGridSupplyPointId(id);
                var pssDict=new Dictionary<PrimarySubstation,int>();
                foreach( var boundary in boundaries) {
                    var numCustomers=0;
                    foreach( var pss in psss ) {
                        var lat = pss.GISData.Latitude;
                        var lng = pss.GISData.Longitude;
                        if( GISUtilities.IsPointInPolygon(lat,lng, boundary.Latitudes,boundary.Longitudes )) {
                            numCustomers += da.Substations.GetCustomersForPrimarySubstation(pss.Id);
                            if ( pssDict.ContainsKey(pss)) {
                                pssDict[pss]+=1;
                            } else {
                                pssDict.Add(pss,1);
                            }
                        }
                    }
                    if ( numCustomers>0 ) {
                        var rD = new RegionData(id,RegionType.GSP);
                        rD.polygon=new Polygon(boundary.Longitudes,boundary.Latitudes);
                        rD.numCustomers = numCustomers;
                        evDi.regionData.Add(rD);
                    } 
                }
                //
                foreach( var pss in psss) {
                    if ( !pssDict.ContainsKey(pss)) {
                        Logger.Instance.LogInfoEvent($"pss [{pss.Name}],[{pss.Id}] not in any GSP boundary");
                    } else if ( pssDict[pss]>1) {
                        Logger.Instance.LogInfoEvent($"pss [{pss.Name}],[{pss.Id}] in multiple GSP boundaries");
                    }
                }
            } else {
                throw new Exception($"No boundaries defined for gsp=[{gsp.Name}]");
            }
        }
        return evDi;
    }
}