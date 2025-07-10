using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class SubstationGISFinder
    {
        private DataAccess _da;
        private GoogleMapsGISFinder _gisFinder;
        private GoogleMapsGISFinder.Geometry _areaGeometry;
        private GeographicalArea _ga;
        private double _tolInKm;
        public SubstationGISFinder(DataAccess da, GeographicalArea ga)
        {
            _da = da;
            _ga = ga;
            _tolInKm = 100;
            _gisFinder = new GoogleMapsGISFinder();
            //
            _areaGeometry = _gisFinder.Lookup($"{ga.Name},UK");
            if ( _areaGeometry==null) {
                throw new Exception($"Cannot find area geometry for area [{ga.Name}]");
            }
        }

        public void Find()
        {
            var dsss = _da.Substations.GetDistributionSubstationsWithNoGIS(_ga);
            foreach( var dss in dsss) {
                var address = $"{dss.Name},{_ga.Name},UK";
                try {
                    var geometry = _gisFinder.Lookup(address);
                    if (geometry != null) {
                        if ( isWithinTolerance(geometry.location) ) {
                            dss.GISData.Latitude = geometry.location.lat;
                            dss.GISData.Longitude = geometry.location.lng;
                            Logger.Instance.LogInfoEvent($"Found GIS data for [{dss.Name}] [{dss.GISData.Latitude} {dss.GISData.Longitude}]");
                        } else {
                            Logger.Instance.LogInfoEvent($"GIS outside area [{_ga.Name}] [{dss.Name}] [{geometry.location.lat} {geometry.location.lng}]");
                        }
                    }
                    else {
                        Logger.Instance.LogInfoEvent($"No GIS data for [{dss.Name}]");
                    }
                }
                catch (Exception e) {
                    Logger.Instance.LogInfoEvent($"Exception getting GIS data for [{dss.Name}] [{e.Message}]");
                }
            }
        }

        private bool isWithinTolerance(GoogleMapsGISFinder.Location location)
        {
            var _areaCenter = _areaGeometry.location;
            return GISUtilities.Distance(_areaCenter.lat,_areaCenter.lng, location.lat, location.lng) < _tolInKm;
        }

    }

}
