// DoAnTotNghiep.Services.Streaming/ControlWebSocketManager.cs
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Đảm bảo đã include

namespace DoAnTotNghiep.Services.Streaming
{
    public class ControlWebSocketManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _controlConnections = new();
        private readonly ILogger<ControlWebSocketManager> _logger;
        private bool _disposed = false;

        public ControlWebSocketManager(ILogger<ControlWebSocketManager> logger)
        {
            _logger = logger;
        }

        public void AddControlConnection(Guid userId, WebSocket webSocket)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ControlWebSocketManager));

            if (_controlConnections.TryAdd(userId, webSocket))
            {
                _logger.LogInformation("Đã đăng ký kết nối Control WebSocket cho người dùng: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Không thể thêm kết nối Control WebSocket cho người dùng {UserId}. Có thể đã tồn tại.", userId);
            }
        }

        public void RemoveControlConnection(Guid userId)
        {
            if (_controlConnections.TryRemove(userId, out var webSocket))
            {
                _logger.LogInformation("Đã xóa kết nối Control WebSocket cho người dùng: {UserId}", userId);
                try
                {
                    if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                    {
                        webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                    }
                    webSocket.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đóng/giải phóng Control WebSocket cho người dùng {UserId}", userId);
                }
            }
        }

        public async Task<bool> SendControlCommandAsync(Guid userId, string command)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ControlWebSocketManager));

            if (_controlConnections.TryGetValue(userId, out var webSocket))
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        var buffer = Encoding.UTF8.GetBytes(command);
                        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        _logger.LogInformation("Đã gửi lệnh '{Command}' đến người dùng {UserId} qua Control WebSocket.", command, userId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi lệnh '{Command}' đến người dùng {UserId} qua Control WebSocket.", command, userId);
                        // Nếu gửi lỗi, có thể kết nối đã chết, hãy xóa nó
                        RemoveControlConnection(userId);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("Control WebSocket cho người dùng {UserId} không ở trạng thái Open. Trạng thái: {State}", userId, webSocket.State);
                    RemoveControlConnection(userId); // Xóa kết nối không hợp lệ
                    return false;
                }
            }
            _logger.LogWarning("Không tìm thấy Control WebSocket cho người dùng {UserId}.", userId);
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var kvp in _controlConnections)
            {
                try
                {
                    if (kvp.Value.State == WebSocketState.Open || kvp.Value.State == WebSocketState.CloseReceived)
                    {
                        kvp.Value.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Application shutting down", CancellationToken.None).Wait(1000);
                    }
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi dispose Control WebSocket cho người dùng {UserId}.", kvp.Key);
                }
            }
            _controlConnections.Clear();
            _logger.LogInformation("ControlWebSocketManager đã được dispose và tất cả kết nối đã đóng.");
        }
    }
}