using CommonInterfaces.Models;
using HaloSoft.EventLogger;

namespace CommonInterfaces.Clients;

public class EVDemandClient {

    private HttpApiClient _httpApiClient;

    public EVDemandClient(Context appContext) {
        string baseUrl = "";
        if ( appContext == Context.Development || appContext == Context.Staging) {
            baseUrl = "http://localhost:5134";
        } else {
            baseUrl = "http://ev-demand.net-zero-energy-systems.org";
        }
        _httpApiClient = new HttpApiClient(baseUrl);
        Admin = new AdminClient(_httpApiClient);
        Predictor = new PredictorClient(_httpApiClient);
    }

    public class AdminClient {
        private HttpApiClient _httpApiClient;

        public AdminClient(HttpApiClient httpApiClient) {
            _httpApiClient = httpApiClient;
        }

        public object Status() {
            return _httpApiClient.Get<object>("/Admin/Status");
        }

        public void Restart() {
            _httpApiClient.Get("/Admin/Restart");
        }

        public LogData Logs() {
            return _httpApiClient.Get<LogData>("/Admin/Logs");
        }
    }

    public class PredictorClient {
        private HttpApiClient _httpApiClient;

        public PredictorClient(HttpApiClient httpApiClient) {
            _httpApiClient = httpApiClient;
        }

        public string Run(EVDemandInput input) {
            var resp = _httpApiClient.Post("/Predictor/Run",input);
            Logger.Instance.LogInfoEvent("Response from Ev predictor");
            Logger.Instance.LogInfoEvent(resp);
            return resp;
        }
    }

    public AdminClient Admin { get; private set;}
    public PredictorClient Predictor {get; private set;}

}