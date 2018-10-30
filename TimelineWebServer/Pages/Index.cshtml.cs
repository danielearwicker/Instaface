using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TimelineWebServer.Pages
{
    public class TimelineModel : PageModel
    {
        public int Id { get; set; }

        public TimelineContainer Data { get; private set; }
        
        public double Network { get; private set; }
        public double Logic { get; private set; }
        public double Fetch { get; private set; }

        public async Task OnGet()
        {
            Id = int.Parse(Request.Query["id"].FirstOrDefault() ?? "1");
            
            
        }
    }
}