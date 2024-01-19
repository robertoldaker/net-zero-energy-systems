using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;

namespace CommonInterfaces.Clients;

public class HttpApiClient
{
    private HttpClient _httpClient;
    public HttpApiClient(string baseAddress) {
        _httpClient = new HttpClient() {
            BaseAddress = new Uri(baseAddress)
        };
    }

    public T Get<T>(string method, params string[] queryParams) where T : class
    {
        T data = null;
        //
        HttpResponseMessage response;
        //
        method = appendQueryString(method, queryParams);
        //
        var client = _httpClient;
        //
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, method)) {
            //
            response = client.SendAsync(message).Result;
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

    public string Get(string method, params string[] queryParams)
    {
        string data;
        //
        HttpResponseMessage response;
        //
        method = appendQueryString(method, queryParams);
        //
        var client = _httpClient;
        //
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, method)) {
            //
            response = client.SendAsync(message).Result;
        }
        //
        if (response.IsSuccessStatusCode) {
            data = response.Content.ReadAsStringAsync().Result;
        } else {
            var str = response.Content.ReadAsStringAsync().Result;
            var message = $"Problem calling method [{method}] [{response.StatusCode}] [{response.ReasonPhrase}] [{str}]";
            Logger.Instance.LogErrorEvent(message);
            throw new Exception(message);
        }
        return data;
    }

    public T Post<T>(string method, object data) where T : class
    {
        T obj = null;
        HttpResponseMessage response;
        //
        var client = _httpClient;
        //
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Post, method, data)) {
            //
            response = client.SendAsync(message).Result;
        }

        //
        if (response.IsSuccessStatusCode) {
            var str = response.Content.ReadAsStringAsync().Result;
            obj = JsonSerializer.Deserialize<T>(str);
        } else {
            var str = response.Content.ReadAsStringAsync().Result;
            Logger.Instance.LogErrorEvent($"Problem calling api method [{method}] [{response.ReasonPhrase}] [{str}]");
            throw new Exception(str);
        }
        return obj;
    }

    public string Post(string method, object data)
    {
        string resp;
        HttpResponseMessage response;
        //
        var client = _httpClient;
        //
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Post, method, data)) {
            //
            response = client.SendAsync(message).Result;
        }

        //
        if (response.IsSuccessStatusCode) {
            resp = response.Content.ReadAsStringAsync().Result;
        } else {
            var str = response.Content.ReadAsStringAsync().Result;
            Logger.Instance.LogErrorEvent($"Problem calling api method [{method}] [{response.ReasonPhrase}] [{str}]");
            throw new Exception(str);
        }
        return resp;
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
