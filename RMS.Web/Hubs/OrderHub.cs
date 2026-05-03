using Microsoft.AspNetCore.SignalR;

namespace RMS.Web.Hubs
{
    public class OrderHub : Hub
    {
        public async Task NotifyNewOrder(int orderId)
        {
            await Clients.All.SendAsync("NewOrderReceived", orderId);
        }

        public async Task NotifyOrderStatusUpdate(int orderId, string status)
        {
            await Clients.All.SendAsync("OrderStatusUpdated", orderId, status);
        }
    }
}
