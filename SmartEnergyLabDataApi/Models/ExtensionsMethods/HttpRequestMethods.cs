using System.Reflection;
using System.Web;

namespace SmartEnergyLabDataApi.Models;

public static class HttpRequestMethods {
    public static string GetFullyQualifiedUrl(this HttpRequest r, string action, object? ps=null)
    {
        //?? Can;t get this to work with test.angelbooks.biz so resorted to hardwiring to https
        string url = r.Scheme + "://" + r.Host;
        //??string url = "https://" + r.Host;
        url += action;
        //
        if (ps != null)
        {
            url += "?";
            Type t = ps.GetType();

            PropertyInfo[] pi = t.GetProperties();
            bool first = true;
            foreach (PropertyInfo p in pi)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    url += "&";
                }
                url += HttpUtility.HtmlEncode(p.Name) + "=" + HttpUtility.HtmlEncode(p.GetValue(ps, null).ToString());
            }
        }
        return url;
    }

}