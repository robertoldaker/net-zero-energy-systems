using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using NHibernate.Criterion;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class NorthernPowerGridLoader {
    private HttpClient _httpClient;
    private object _httpClientLock = new object();
    private string _baseUrl = "https://northernpowergrid.opendatasoft.com"; 
    // key generated from the UK power networks opn data website
    // https://ukpowernetworks.opendatasoft.com/account/api-keys/
    private string _apiKey = "dd2fce73b6487e3d3943580c048231b8b0518d0169db407ae981e7bc";
    private TaskRunner? _taskRunner; 
    private Dictionary<string,string> _upstreamSiteAliases = new Dictionary<string, string>() {
        { "Fourstones" , "Fourstones 33Kv" },
        { "Linton", "Blyth 132Kv"},
        { "Blyth 132/66Kv", "Blyth 66Kv"},
        { "Spennymoor", "Spennymoor Gsp"},
        { "Bradford", "Bradford West"},
    };

    public NorthernPowerGridLoader(TaskRunner? taskRunner) {
        _taskRunner = taskRunner;
    }


    public void Load() {
            var url = "/api/explore/v2.1/catalog/datasets/npg-site-utilisation/exports/json?lang=en&timezone=Europe%2FLondon";
            var allRecords = get<List<NPGSiteUtilisationRecord>>(url);

            // Process GSPs
            var gspRecords = allRecords.Where(m=>m.substation_class=="Grid Supply Point").ToList();
            // From email with NPG it appears that primaries/BSPs with associated_upstream_site == site_name mean they are actually connected to grid
            // so am adding them as extra GSPs
            var extraRecords = allRecords.Where( m=>m.site_name==m.associated_upstream_site && m.substation_class!="Distribution" && gspRecords.Where(n=>n.site_name==m.site_name).Count()==0).ToList();
            gspRecords.AddRange(extraRecords);
            processGspRecords(gspRecords);

            // Process primaries
            var primaryRecords = allRecords.Where(m=>m.substation_class=="Primary" || m.substation_class=="Bulk Supply Point").ToList();
            processPrimaryRecords(primaryRecords, allRecords);

            // Process distrubtion
            var distRecords = allRecords.Where(m=>m.substation_class=="Distribution").ToList();
            processDistRecords(distRecords);
    }

    private void processGspRecords(List<NPGSiteUtilisationRecord> records) {
        updateMessage("Processing GSP records ...");
        using ( var da = new DataAccess()) {
            int nAdded=0;
            var dno = da.Organisations.GetDistributionNetworkOperator(DNOCode.NorthernPowerGrid);
            foreach( var record in records) {
                var gsp = da.SupplyPoints.GetGridSupplyPointByName(record.site_name);
                if ( gsp==null) {
                    var ga = getGeographicalArea(da, record);
                    gsp = new GridSupplyPoint(ImportSource.NorthernPowerGridOpenData,record.site_name,null,null,ga,dno);
                    da.SupplyPoints.Add(gsp);
                    nAdded++;
                }
                // update fields
                gsp.GISData.Latitude = record.geopoint.lat;
                gsp.GISData.Longitude = record.geopoint.lon;
            }
            da.CommitChanges();
            Logger.Instance.LogInfoEvent($"[{nAdded}] GSPs added");
        }
    }


    private void processPrimaryRecords(List<NPGSiteUtilisationRecord> primRecords, List<NPGSiteUtilisationRecord> allRecords) {
        updateMessage("Processing Primary records ...");        
        using ( var da = new DataAccess()) {
            int nAdded=0;
            var dno = da.Organisations.GetDistributionNetworkOperator(DNOCode.NorthernPowerGrid);
            foreach( var primRecord in primRecords) {
                var gsp = getGridSupplyPoint(da,primRecord,allRecords);
                if ( gsp!=null) {
                    var pss = da.Substations.GetPrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,primRecord.site_name);
                    if ( pss==null) {
                        pss = new PrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,gsp);
                        pss.Name = primRecord.site_name;
                        da.Substations.Add(pss);
                        nAdded++;
                    }
                    // update fields
                    pss.GISData.Latitude = primRecord.geopoint.lat;
                    pss.GISData.Longitude = primRecord.geopoint.lon;
                } else {
                    Logger.Instance.LogWarningEvent($"Cannot find associated upstream site with name [{primRecord.associated_upstream_site}] for primary [{primRecord.site_name}]");
                }
            }
            da.CommitChanges();
            updateMessage($"[{nAdded}] primary substations added");
        }
    }

    private string getLocationKey(NPGSiteUtilisationRecord record) {
        return $"{record.geopoint.lat:F6}:{record.geopoint.lon:F6}";
    }

    private GridSupplyPoint getGridSupplyPoint(DataAccess da, NPGSiteUtilisationRecord primRecord, List<NPGSiteUtilisationRecord> allRecords) {
        // First see if we have a GSP in the db
        var gsp = da.SupplyPoints.GetGridSupplyPointByName(primRecord.associated_upstream_site);
        if ( gsp==null ) {
            // see if its connected to a BSP and use this??
            var bspRecord = allRecords.Where( m=>m.site_name == primRecord.associated_upstream_site).FirstOrDefault();
            if ( bspRecord!=null) {
                gsp = da.SupplyPoints.GetGridSupplyPointByName(bspRecord.associated_upstream_site);
            }
        }
        if ( gsp==null) {
            // Some entries use the wrong name so see if the name is in the list of upstream aliases
            if ( _upstreamSiteAliases.ContainsKey(primRecord.associated_upstream_site) ) {
                var site_alias = _upstreamSiteAliases[primRecord.associated_upstream_site];
                gsp = da.SupplyPoints.GetGridSupplyPointByName(site_alias);
            }
        }
        return gsp;
    }

    private GeographicalArea getGeographicalArea(DataAccess da, NPGSiteUtilisationRecord record) {
        if ( record.licence_area == "Yorkshire" ) {
            return da.Organisations.GetGeographicalArea(DNOAreas.Yorkshire);
        } else if ( record.licence_area == "Northeast") {
            return da.Organisations.GetGeographicalArea(DNOAreas.NorthEastEngland);
        } else {
            throw new Exception($"Unexpected license area for NPG import [{record.licence_area}]");
        }
    }

    private void processDistRecords(List<NPGSiteUtilisationRecord> distRecords) {
        int nAdded = 0;
        int nIgnored = 0;
        Dictionary<string,bool> notFound=new Dictionary<string, bool>();
        processSegments<NPGSiteUtilisationRecord>(distRecords,
                        // Note - its quicker to process using 100 instead of 1000
                        100,(loaded,total,records)=>{
                            updateMessage($"Processing NPG site utilisation records [{loaded}] of [{total}]...",false);
                            int percent = (int) (100*loaded)/total;
                            updateProgress(percent);
                            processRecords(records, ref nAdded, ref nIgnored, notFound);
        });
        Logger.Instance.LogInfoEvent($"[{nAdded}] distribution substations added, [{nIgnored}] ignored");
    }

    private void processSegments<T>(List<T> allRecords,int rows=100, Action<int,int,IEnumerable<T>>? progress=null) where T : class {
        IEnumerable<T> records=null;
        int start=0;
        do {
            checkCancelled();
            records = allRecords.Skip(start).Take(rows);
            progress?.Invoke(start,allRecords.Count,records);
            start += rows;
        } while( start<allRecords.Count );
        //
    }

    private void processRecords(IEnumerable<NPGSiteUtilisationRecord> records, ref int nAdded, ref int nIgnored, Dictionary<string,bool> notFound) {
        using ( var da = new DataAccess() ) {
            foreach( var distRecord in records) {
                var pss = getPrimarySubstation(da,distRecord);
                if ( pss!=null) {
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.NorthernPowerGridOpenData,null,null,distRecord.site_name);
                    if ( dss==null) {
                        dss = new DistributionSubstation(ImportSource.NorthernPowerGridOpenData,null,null,pss);
                        dss.Name = distRecord.site_name;
                        da.Substations.Add(dss);    
                        nAdded++;
                    }
                    // update fields
                    dss.GISData.Latitude = distRecord.geopoint.lat;
                    dss.GISData.Longitude = distRecord.geopoint.lon;
                    dss.SubstationData.NumCustomers = distRecord.connected_customers!=null ? (int) Math.Round((double) distRecord.connected_customers) : 0;
                    dss.SubstationData.DayMaxDemand = distRecord.max_demand_kw!=null ? (double) distRecord.max_demand_kw : 0;
                    dss.SubstationData.Rating = distRecord.firm_capacity_kw!=null ? (double) distRecord.firm_capacity_kw : 0;
                    if ( distRecord.substation_type=="HV GM Substation") {
                        dss.SubstationData.Type = DistributionSubstationType.Ground;
                    } else if ( distRecord.substation_type=="HV Pole Mounted S/S") {
                        dss.SubstationData.Type = DistributionSubstationType.Pole;
                    } else {
                        Logger.Instance.LogWarningEvent($"Unexpected substation type found [{distRecord.substation_type}] for dist substation [{distRecord.site_name}]");
                    }
                } else {
                    if ( !notFound.ContainsKey(distRecord.associated_upstream_site)) {
                        Logger.Instance.LogWarningEvent($"Cannot find upstream site with name [{distRecord.associated_upstream_site}]");
                        notFound.Add(distRecord.associated_upstream_site,true);
                    }
                    nIgnored++;
                }                
            }
            //
            da.CommitChanges();
        }
    }

    private PrimarySubstation getPrimarySubstation(DataAccess da, NPGSiteUtilisationRecord distRecord) {
        // First see if we have a Primary in the db
        var pss = da.Substations.GetPrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,distRecord.associated_upstream_site);
        return pss;
    }


    private void checkCancelled() {
        _taskRunner?.CheckCancelled();
    }

    private void updateMessage(string message,bool addToLog=true) {
        _taskRunner?.Update(message,addToLog);
    }

    private void updateProgress(int percent) {
        _taskRunner?.Update(percent);
    }

    private class GeoPoint {
        public double lat {get; set;}
        public double lon {get; set;}
    }

    private class NPGSiteUtilisationRecord {
        public string site_name { get; set;}
        public string eam_site_asset_id {get; set;}
        public string substation_type {get; set;}
        public string licence_area {get; set;}
        public double? max_demand_kw {get; set;}
        public double? connected_customers {get; set;}
        public double? firm_capacity_kw {get; set;}
        public string substation_class {get; set;}
        public string associated_upstream_site {get; set;}
        public GeoPoint geopoint {get; set;}
    }

    private T get<T>(string method, params string[] queryParams) where T : class
    {
        T data = null;
        //
        HttpResponseMessage response;
        //
        method = appendQueryString(method, queryParams);
        //
        var client = getHttpClient();
        //
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, method)) {
            //
            response = client.SendAsync(message).Result;
            //
        }
        //
        if (response.IsSuccessStatusCode) {
            var str = response.Content.ReadAsStringAsync().Result;
            data = JsonSerializer.Deserialize<T>(str);

        } else {
            var str = response.Content.ReadAsStringAsync().Result;
            var message = $"Problem calling method [{method}] [{response.StatusCode}] [{response.ReasonPhrase}] [{str}]";
            Logger.Instance.LogErrorEvent(message);
            throw new Exception(message);
        }
        return data;
    }

    private HttpClient getHttpClient()
    {
        if (_httpClient == null) {
            lock (_httpClientLock) {
                _httpClient = new HttpClient() {
                    BaseAddress = new Uri(_baseUrl)
                };
                _httpClient.DefaultRequestHeaders.Add("Authorization",$"Apikey {_apiKey}");
            }
        }
        //
        return _httpClient;
    }

    private HttpRequestMessage getRequestMessage(HttpMethod httpMethod, string method, object data = null)
    {
        HttpRequestMessage message = new HttpRequestMessage(httpMethod, method);
        if (data != null) {
            string reqStr;
            if (data is string) {
                reqStr = (string)data;
            } else {
                reqStr = JsonSerializer.Serialize(data);
            }
            message.Content = new StringContent(reqStr, Encoding.UTF8, "application/json");
        }
        return message;
    }


    private static string appendQueryString(string method, params string[] nameValuePairs)
    {
        string s = method;
        //
        if (nameValuePairs.Length > 0) {
            s += "?" + getNameValuePairs(nameValuePairs);
        }
        return s;
    }

    private static string appendQueryString(string method, Dictionary<string, string> dict)
    {
        string s = method;
        //
        bool isFirst = true;
        foreach (var d in dict) {
            if (isFirst) {
                s += "?";
                isFirst = false;
            } else {
                s += "&";
            }
            s += Uri.EscapeDataString(d.Key) + "=" + Uri.EscapeDataString(d.Value);
        }
        return s;
    }

    private static string getNameValuePairs(params string[] nameValuePairs)
    {
        //
        if ((nameValuePairs.Length % 2) != 0) {
            throw new Exception("Wrong number of parameters");
        }
        string[] strs = new string[nameValuePairs.Length / 2];
        int count = 0;
        for (int index = 0; index < nameValuePairs.Length - 1; index += 2) {
            //
            string name = nameValuePairs[index];
            if (name == null) { name = ""; }
            string value = nameValuePairs[index + 1];
            if (value == null) { value = ""; }
            strs[count++] = Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);
        }
        //
        return string.Join("&", strs);
    }

}