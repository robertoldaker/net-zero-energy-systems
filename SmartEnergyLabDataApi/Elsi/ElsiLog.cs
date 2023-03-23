

using Microsoft.AspNetCore.SignalR;

namespace SmartEnergyLabDataApi.Elsi
{
    public class ElsiLog {
        private string _connectionId;
        private IHubContext<NotificationHub> _hubContext;
        public ElsiLog(IHubContext<NotificationHub> hubContext, string connectionId) {
            _hubContext = hubContext;
            _connectionId = connectionId;
        }
        public void WriteVars(params object[] vars) {
            int i=0;
            string str = "";
            foreach( var v in vars) {
                if ( v is double) {
                    str+=$"{v:n8} ";
                } else if ( v is int) {
                    str+=$"{v} ";
                } else {
                    str+=$"{v}";
                    if ( i%2 == 0 && vars.Length>1 ) {
                        str+="=";
                    } else {
                        str+=" ";
                    }
                }
                i++;
            }
            _hubContext.Clients.Client(_connectionId).SendAsync("Elsi_Log",str);
        }

        public void WriteStr(string str) {
            _hubContext.Clients.Client(_connectionId).SendAsync("Elsi_Log",str);
        }
    }
}