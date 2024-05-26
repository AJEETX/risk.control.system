//using System.Net.WebSockets;
//using System.Text;

//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace risk.control.system.Controllers
//{
//    [Route("ws")]
//    [ApiExplorerSettings(IgnoreApi = true)]
//    [ApiController]
//    public class WebSocketController : ControllerBase
//    {
//        private readonly Dictionary<string, WebSocket> _clients = new Dictionary<string, WebSocket>();

//        public async Task Get()
//        {
//            if (HttpContext.WebSockets.IsWebSocketRequest)
//            {
//                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
//                await HandleWebSocket(webSocket);
//            }
//            else
//            {
//                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
//            }
//        }

//        private async Task HandleWebSocket(WebSocket webSocket)
//        {
//            // Generate a unique client ID
//            string clientId = Guid.NewGuid().ToString();

//            // Add WebSocket object to dictionary
//            _clients.Add(clientId, webSocket);

//            // Handle incoming messages
//            await ReceiveMessages(clientId, webSocket);

//            // Remove WebSocket object from dictionary when connection is closed
//            _clients.Remove(clientId);
//        }

//        private async Task ReceiveMessages(string clientId, WebSocket webSocket)
//        {
//            byte[] buffer = new byte[1024 * 4];
//            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

//            while (!result.CloseStatus.HasValue)
//            {
//                // Process incoming message (optional)
//                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

//                // Example: Echo message back to client
//                await SendMessage(clientId, "Server received: " + message);

//                // Receive next message
//                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//            }
//        }

//        private async Task SendMessage(string clientId, string message)
//        {
//            if (_clients.TryGetValue(clientId, out WebSocket clientSocket))
//            {
//                byte[] buffer = Encoding.UTF8.GetBytes(message);
//                await clientSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
//            }
//        }

//        private static async Task Echo(WebSocket webSocket)
//        {
//            var buffer = new byte[1024 * 4];
//            var receiveResult = await webSocket.ReceiveAsync(
//                new ArraySegment<byte>(buffer), CancellationToken.None);

//            while (!receiveResult.CloseStatus.HasValue)
//            {
//                await webSocket.SendAsync(
//                    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
//                    receiveResult.MessageType,
//                    receiveResult.EndOfMessage,
//                    CancellationToken.None);

//                receiveResult = await webSocket.ReceiveAsync(
//                    new ArraySegment<byte>(buffer), CancellationToken.None);
//            }

//            await webSocket.CloseAsync(
//                receiveResult.CloseStatus.Value,
//                receiveResult.CloseStatusDescription,
//                CancellationToken.None);
//        }
//    }
//}
