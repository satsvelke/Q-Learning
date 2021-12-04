using Microsoft.Owin;
using Owin;
using TicTacToe;

[assembly: OwinStartup(typeof(TicTacToe.Startup))]
namespace TicTacToe
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}


