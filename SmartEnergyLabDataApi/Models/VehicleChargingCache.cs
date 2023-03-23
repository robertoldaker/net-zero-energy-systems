using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class VehicleChargingCache
    {
        private Dictionary<string, VehicleChargingStation> _stationDict;
        private Dictionary<string, VehicleChargingConnectionType> _connectionTypeDict;
        private Dictionary<string, VehicleChargingCurrentType> _currentTypeDict;
        public VehicleChargingCache(DataAccess da, GeographicalArea ga)
        {
            Logger.Instance.LogInfoEvent("Loading cache");
            //
            _stationDict = new Dictionary<string, VehicleChargingStation>();
            var psss = da.VehicleCharging.GetVehicleChargingStations(ga.Id);
            foreach( var pss in psss) {
                _stationDict.Add(pss.ExternalId,pss);
            }
            // 
            _connectionTypeDict = new Dictionary<string, VehicleChargingConnectionType>();
            var dsss = da.VehicleCharging.GetVehicleChargingConnectionTypes();
            foreach( var dss in dsss) {
                _connectionTypeDict.Add(dss.ExternalId, dss);
            }
            // 
            _currentTypeDict = new Dictionary<string, VehicleChargingCurrentType>();
            var ssCs = da.VehicleCharging.GetVehicleChargingCurrentTypes();
            foreach (var ssC in ssCs) {
                _currentTypeDict.Add(ssC.ExternalId, ssC);
            }
            Logger.Instance.LogInfoEvent("Cache loaded");
        }

        public void Add(VehicleChargingStation vcs)
        {
            _stationDict.Add(vcs.ExternalId, vcs);
        }

        public void Add(VehicleChargingConnectionType vcct)
        {
            _connectionTypeDict.Add(vcct.ExternalId, vcct);
        }

        public void Add(VehicleChargingCurrentType vcct)
        {
            _currentTypeDict.Add(vcct.ExternalId, vcct);
        }

        public VehicleChargingStation GetVehicleChargingStation(string externalId)
        {
            VehicleChargingStation vcct = null;
            if (string.IsNullOrEmpty(externalId)) {
                return null;
            }
            _stationDict.TryGetValue(externalId, out vcct);
            return vcct;
        }

        public VehicleChargingConnectionType GetVehicleChargingConnectionType(string externalId)
        {
            VehicleChargingConnectionType vcct = null;
            if (string.IsNullOrEmpty(externalId)) {
                return null;
            }
            _connectionTypeDict.TryGetValue(externalId, out vcct);
            return vcct;
        }
        public VehicleChargingCurrentType GetVehicleChargingCurrentType(string externalId)
        {
            VehicleChargingCurrentType vcct = null;
            if (string.IsNullOrEmpty(externalId)) {
                return null;
            }
            _currentTypeDict.TryGetValue(externalId, out vcct);
            return vcct;
        }
    }

}