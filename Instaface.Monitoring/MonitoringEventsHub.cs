namespace Instaface.Monitoring
{
    using Microsoft.AspNetCore.SignalR;

    public class MonitoringEventsHub : Hub<IMonitoringEvents> { }
}
