using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DoAnTotNghiep.Services.Streaming
{
    public class WebSocketManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, ConnectionInfo> _connections = new();
        private readonly ILogger<WebSocketManager> _logger;
        private readonly Timer _cleanupTimer;
        private const int REQUEST_TIMEOUT_MS = 30000;
        private const int CONNECTION_TIMEOUT_MINUTES = 30;
        private bool _disposed = false;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;
            _cleanupTimer = new Timer(CleanupStaleConnections, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public Guid RegisterConnection(WebSocket webSocket)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WebSocketManager));

            var connectionId = Guid.NewGuid();
            var connectionInfo = new ConnectionInfo(webSocket, DateTime.UtcNow);

            if (_connections.TryAdd(connectionId, connectionInfo))
            {
                _logger.LogInformation("Registered WebSocket connection: {ConnectionId}", connectionId);
                return connectionId;
            }

            throw new InvalidOperationException("Failed to register WebSocket connection");
        }

        public async Task<bool> RequestViewAsync(Guid userId)
        {
            if (!_connections.TryGetValue(userId, out var connectionInfo))
            {
                _logger.LogWarning("No active connection found for user {UserId}", userId);
                return false;
            }

            var webSocket = connectionInfo.WebSocket;

            try
            {
                if (webSocket.State != WebSocketState.Open)
                {
                    _connections.TryRemove(userId, out _);
                    return false;
                }

                var request = new
                {
                    type = "REQUEST_VIEW",
                    timestamp = DateTime.UtcNow,
                    requestId = Guid.NewGuid()
                };

                var message = JsonSerializer.Serialize(request);
                var buffer = Encoding.UTF8.GetBytes(message);

                using var cts = new CancellationTokenSource(REQUEST_TIMEOUT_MS);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                _logger.LogInformation("View request sent to user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send view request to user {UserId}", userId);
                _connections.TryRemove(userId, out _);
                return false;
            }
        }

        public async Task HandleStudentStreamAsync(WebSocket webSocket, Stream pipeStream, string connectionId, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.Count > 0)
                    {
                        await pipeStream.WriteAsync(buffer, 0, result.Count, cancellationToken);
                    }
                }
            }
            finally
            {
                // Đóng PipeStream báo hiệu EOF cho FFmpeg
                pipeStream.Dispose();
            }
        }



        private async Task HandleTextMessage(WebSocket webSocket, byte[] buffer, int count,
            CancellationToken cancellationToken)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, count);
            _logger.LogDebug("Received text message: {Message}", message);

            try
            {
                var messageObj = JsonSerializer.Deserialize<JsonElement>(message);

                if (messageObj.TryGetProperty("type", out var typeElement) &&
                    typeElement.GetString() == "ACK_VIEW_REQUEST")
                {
                    var response = new { type = "ACK_RECEIVED", timestamp = DateTime.UtcNow };
                    var responseBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(responseBuffer),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON message received: {Message}", message);
            }
        }

        private void CleanupStaleConnections(object? state)
        {
            if (_disposed) return;

            var cutoff = DateTime.UtcNow.AddMinutes(-CONNECTION_TIMEOUT_MINUTES);
            var staleConnections = _connections
                .Where(kvp => kvp.Value.CreatedAt < cutoff || kvp.Value.WebSocket.State != WebSocketState.Open)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var connectionId in staleConnections)
            {
                if (_connections.TryRemove(connectionId, out var connectionInfo))
                {
                    try
                    {
                        connectionInfo.WebSocket.Dispose();
                    }
                    catch { }

                    _logger.LogInformation("Cleaned up stale connection {ConnectionId}", connectionId);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cleanupTimer?.Dispose();

            foreach (var connection in _connections.Values)
            {
                try
                {
                    connection.WebSocket.Dispose();
                }
                catch { }
            }

            _connections.Clear();
        }

        public void RemoveConnection(Guid connectionId)
        {
            if (_connections.TryRemove(connectionId, out var connectionInfo))
            {
                try
                {
                    if (connectionInfo.WebSocket.State == WebSocketState.Open)
                    {
                        // Fire and forget the close operation
                        _ = connectionInfo.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection removed",
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing WebSocket for connection {ConnectionId}", connectionId);
                }
                finally
                {
                    _logger.LogInformation("Removed WebSocket connection: {ConnectionId}", connectionId);
                }
            }
        }

        private record ConnectionInfo(WebSocket WebSocket, DateTime CreatedAt);
    }
}