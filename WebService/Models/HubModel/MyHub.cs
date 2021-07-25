using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService.Models.HubModel
{
    [HubName("myHub")]
    public class MyHub : Hub
    {
        [HubMethodName("sendEployees")]
        public static void SendEployees()
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            context.Clients.All.updateMessages();
        }
    }
}