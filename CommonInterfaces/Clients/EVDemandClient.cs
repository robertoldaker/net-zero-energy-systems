using CommonInterfaces.Models;

namespace CommonInterfaces.Clients;

public class EVDemandClient {

    private HttpApiClient _httpApiClient;

    public EVDemandClient() {
        _httpApiClient = new HttpApiClient("http://localhost:5134");
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
            return _httpApiClient.Post("/Predictor/Run",input);
        }
    }

    public AdminClient Admin { get; private set;}
    public PredictorClient Predictor {get; private set;}

}