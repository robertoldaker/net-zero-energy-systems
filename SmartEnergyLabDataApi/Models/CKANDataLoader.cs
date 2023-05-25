using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models
{
    public class CKANDataLoader {

        private HttpClient _httpClient;
        private string _baseUrl;
        private string _packageName;
        private string _packageUrl;
        private CKANDatasets _datasets;
        private static object _httpClientLock = new object();

        public CKANDataLoader(string baseUrl, string packageName) {
            _baseUrl = baseUrl;
            _packageName = packageName;
            _packageUrl = $"/dataset/{packageName}/datapackage.json";
            _datasets = get<CKANDatasets>(_packageUrl);
        }


        public List<T> LoadAll<T>(string datasetName, Action<int>? progress=null) {
            var list = new List<T>();
            var limit = 5000;
            var initial = LoadInitial<T>(datasetName, limit);
            int percent=addContainer(list,initial,datasetName);
            progress?.Invoke(percent);
            var total = initial.result.total;
            var next = initial;
            while ( list.Count<total) {
                next = LoadNext<T>(next);
                addContainer(list,next,datasetName);
                progress?.Invoke(percent);
            }
            return list;
        }

        private int addContainer<T>(List<T> list, DatasetContainer<T> container, string datasetName) {
            if ( container.success ) {
                list.AddRange(container.result.records);
                return (list.Count*100)/container.result.total;
            } else {
                throw new Exception($"Had unsuccessful load for dataset [{datasetName}], package name [{_packageName}]");
            }
        }

        public DatasetContainer<T> LoadInitial<T>(string datasetName, int limit) {
            var dataset = _datasets.GetByName(datasetName);
            var dis = get<DatasetContainer<T>>("/api/3/action/datastore_search","resource_id",dataset.id,"limit",limit.ToString());
            return dis;
        }

        public DatasetContainer<T> LoadNext<T>(DatasetContainer<T> container) {
            return get<DatasetContainer<T>>(container.result._links.next);            
        }

        public CKANDataset GetDatasetInfo(string name) {
            var dataset = _datasets.GetByName(name);
            if ( dataset==null  ) {
                throw new Exception($"No dataset found with name [{name}] for CKAN package [{_packageName}]");
            } else {
                return dataset;
            }
        }

        private class CKANDatasets {
            public CKANDataset[] resources {get; set;}

            public CKANDataset GetByName(string name) {
                return resources.Where( m=>m.name == name).FirstOrDefault();
            }
        }

        public class CKANDataset {
            public string name {get; set;}
            public string id {get; set;}
            public string format {get; set;}
            public DateTime last_modified {get; set;}
            public string url {get; set;}

            public bool NeedsImport( DateTime? lastModified) {
                if ( lastModified!=null ) {
                    return lastModified < last_modified;
                } else {
                    return true;
                }
            }            
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
                    _httpClient = new HttpClient();
                    _httpClient.BaseAddress = new Uri(_baseUrl);
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

        public class DatasetContainer<T> {
            public bool success {get; set;}
            public DatasetResult<T> result {get ;set; }

        }

        public class DatasetResult<T> {
            public int total {get; set;}
            public bool total_was_estimated {get ;set;}
            public DatasetLinks _links {get; set;}
            public List<T> records {get; set;}

        }

        public class DatasetLinks {
            public string start {get; set;}
            public string next {get; set;}
        }

        
    }
}