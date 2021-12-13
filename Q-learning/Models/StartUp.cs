using Microsoft.Owin;
using Owin;
using QLearning;

[assembly: OwinStartup(typeof(QLearning.Startup))]
namespace QLearning
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}


