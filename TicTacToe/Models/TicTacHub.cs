using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace TicTacToe.Models
{
    public class TicTacHub : Hub
    {
        public void Names(string name)
        {
            Clients.All.addNames(name);
        }

        public void Addtictac(string name, string action,string id)
        {
            Clients.All.addTics(name, action,id);
        }

        public void Reloadgame(string something)
        {
            Clients.All.reload(something);
        }
    }
}