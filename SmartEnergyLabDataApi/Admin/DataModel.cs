using System;
using System.Xml.Schema;
using HaloSoft.EventLogger;
using Microsoft.Extensions.FileProviders;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace EnergySystemLabDataApi
{
    public class DataModel {

        public void Load() {
            using (var da = new DataAccess() ) {
                DataAccess.RunPostgreSQLQuery("SELECT pg_size_pretty(pg_database_size('smart_energy_lab'));",(row)=>{
                    this.Size = row[0].ToString();
                });
                var geos = da.Organisations.GetGeographicalAreas();
            }
            DiskUsage = new DiskUsage();
        }

        public string Size {get; private set;}
        public int DiskSpaceUsed {get; private set;}
        public DiskUsage DiskUsage {get; private set;}
    }

    public class DiskUsage {
        public DiskUsage() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                getDiskUsageWindows();
            } else {
                getDiskUsageLinux();
            }
        }

        private void getDiskUsageWindows() {
            // Get the drive information of the current drive (you can specify a different drive letter if needed)
            DriveInfo drive = new DriveInfo(Environment.CurrentDirectory);

            Total = (int) (drive.TotalSize / (1024*1024*1024));
            Available = (int) (drive.AvailableFreeSpace / (1024*1024*1024));
            Used = Total - Available;
            Found = true;
        }

        private void getDiskUsageLinux() {
            var com = new Execute();
            int resp =com.Run("df","");
            Found = false;
            // Can generate an error if no sudo privilege but still outputs hat we need
            if ( com.StandardOutput!=null ) {
                var lines = com.StandardOutput.Split('\n');
                foreach( var line in lines) {
                    var cols = line.Split(new char[] {' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
                    if ( cols.Length>=6) {
                        var mountedOn = cols[5];
                        if ( mountedOn=="/") {
                            try {
                                long total = long.Parse(cols[1]);
                                long used = long.Parse(cols[2]);
                                long avail = long.Parse(cols[3]);
                                Used = (int) (used/1048576);
                                Available = (int) (avail/1048576);
                                Total = (int) (total/1048576);
                                Found = true;
                            } catch (Exception e) {
                                Logger.Instance.LogErrorEvent("Problem obtaining disk space");
                                Logger.Instance.LogException(e);
                            }
                        }
                    }
                }
            }
        } 
        public bool Found {get; private set;}
        public int Used {get; private set;}
        public int Available {get; private set;}
        public int Total {get; private set;}
    }

    public class TransmissionData {
        public TransmissionData() {
            Rows = new List<TransmissionDataRow>();
            using (var da = new DataAccess() ) {
                foreach( var src in Enum.GetValues(typeof(GridSubstationLocationSource))) {
                    Rows.Add( new TransmissionDataRow(da, (GridSubstationLocationSource) src));
                }
            }
        }

        public List<TransmissionDataRow> Rows {get; private set;}
    }

    public class TransmissionDataRow {
        public TransmissionDataRow(DataAccess da, GridSubstationLocationSource source) {
            Source = source;
            SourceIconUrl = getIconUrl(source);
            SourceStr = source.ToString();
            NumLocations = da.NationalGrid.GetNumGridSubstationLocations(source);
            var gs = getSubstationSource(source);
            if ( gs!=null ) {
                NumSubstations = da.NationalGrid.GetNumGridSubstations((GridSubstationSource) gs);
            } else {
                NumSubstations = 0;
            }
        }

        GridSubstationSource? getSubstationSource(GridSubstationLocationSource locSrc) {
            if ( locSrc == GridSubstationLocationSource.NGET) {
                return GridSubstationSource.NGET;
            } else if ( locSrc == GridSubstationLocationSource.SHET) {
                return GridSubstationSource.SHET;
            } else if ( locSrc == GridSubstationLocationSource.SPT) {
                return GridSubstationSource.SPT;
            } else {
                return null;
            }
        }

        private string getIconUrl(GridSubstationLocationSource source) {
            string root = "/assets/images/";
            if (  source == GridSubstationLocationSource.NGET) {
                return root + "national-grid-icon.png";
            } else if ( source == GridSubstationLocationSource.SHET ) {
                return root + "scottish-and-southern-electricity-networks-icon.png";
            } else if ( source == GridSubstationLocationSource.SPT) {
                return root + "sp-electricity-networks-icon.png";
            } else if ( source == GridSubstationLocationSource.Estimated) {
                return "";
            } else if ( source == GridSubstationLocationSource.GoogleMaps) {
                return "";
            } else if ( source == GridSubstationLocationSource.UserDefined) {
                return "";
            } else {
                throw new Exception($"Unexpected location source [{source}]");
            }
        }
        public GridSubstationLocationSource Source {get; set;}
        public string SourceStr {get; set;}
        public string SourceIconUrl {get; set;}
        public int NumLocations {get; set;}
        public int NumSubstations {get; set;}
    }

    public class DistributionData {
        public DistributionData() {
            Rows = new List<DistributionDataRow>();
            using (var da = new DataAccess() ) {
                var geos = da.Organisations.GetGeographicalAreas();
                foreach( var geo in geos) {
                    Rows.Add( new DistributionDataRow(da,geo));
                }
            }
        }

        public List<DistributionDataRow> Rows {get; private set;}
    }

    public class DistributionDataRow {
        public DistributionDataRow(DataAccess da, GeographicalArea geo) {
            GeoGraphicalAreaId = geo.Id;
            GeoGraphicalArea = geo.Name;
            DNOIconUrl = getIconUrl(geo.DistributionNetworkOperator);
            DNO = geo.DistributionNetworkOperator.Name;
            NumGsps = da.SupplyPoints.GetNumGridSupplyPoints(geo.Id);
            NumPrimary = da.Substations.GetNumPrimarySubstations(geo.Id);
            NumDist = da.Substations.GetNumDistributionSubstations(geo.Id);
        }

        private string getIconUrl(DistributionNetworkOperator dno) {
            string root = "/assets/images/";
            if (  dno.Code == DNOCode.NationalGridElectricityDistribution) {
                return root + "national-grid-icon.png";
            } else if ( dno.Code == DNOCode.UKPowerNetworks ) {
                return root + "uk-power-networks-icon.png";
            } else if ( dno.Code == DNOCode.ElectricityNorthWest) {
                return root + "electricity-north-west-icon.png";
            } else if ( dno.Code == DNOCode.NorthernPowerGrid) {
                return root + "northern-power-grid-icon.png";
            } else if ( dno.Code == DNOCode.ScottishAndSouthernElectricityNetworks) {
                return root + "scottish-and-southern-electricity-networks-icon.png";
            } else if ( dno.Code == DNOCode.SPEnergyNetworks) {
                return root + "sp-electricity-networks-icon.png";
            } else {
                throw new Exception($"Unexpected DNO code [{dno.Code}] for dno [{dno.Name}]");
            }
        }
        public int GeoGraphicalAreaId {get; set;}
        public string GeoGraphicalArea {get; set;}
        public string DNOIconUrl {get; set;}
        public string DNO {get; set;}
        public int NumGsps {get; set;}
        public int NumPrimary {get; set;}
        public int NumDist {get; set;}
    }

     
}