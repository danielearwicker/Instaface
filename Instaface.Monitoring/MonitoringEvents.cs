namespace Instaface.Monitoring
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json.Linq;

    public class MonitoringEvents : IMonitoringEvents
    {
        private readonly IHubContext<MonitoringEventsHub, IMonitoringEvents> _hub;

        public MonitoringEvents(IHubContext<MonitoringEventsHub, IMonitoringEvents> hub)
        {
            _hub = hub;
        }
        
        public Task Event(JToken info)
        {
            return _hub.Clients.All.Event(info);
        }
    }
}