namespace Instaface.Monitoring
{
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public interface IMonitoringEvents
    {
        Task Event(JToken info);
    }
}