using System.Net.WebSockets;

namespace DoAnTotNghiep.Services.Streaming
{
    public class WebSocketManager
    {
        public async Task HandleStudentStream(
            WebSocket webSocket,
            Stream outputStream,
            CancellationTokenSource cts)
        {
            byte[] buffer = new byte[8192]; // 8KB buffer mỗi lần đọc (không cần 4MB ngay từ đầu)
            var messageBuffer = new MemoryStream();

            try
            {
                while (!cts.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    var segment = new ArraySegment<byte>(buffer);
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await webSocket.ReceiveAsync(segment, cts.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, cts.Token);
                            return;
                        }

                        if (result.MessageType != WebSocketMessageType.Binary)
                            continue;

                        // Ghi dữ liệu nhận được vào memory buffer
                        await messageBuffer.WriteAsync(buffer, 0, result.Count, cts.Token);

                    } while (!result.EndOfMessage && !cts.Token.IsCancellationRequested);

                    if (messageBuffer.Length > 0)
                    {
                        messageBuffer.Position = 0;
                        await messageBuffer.CopyToAsync(outputStream, cts.Token);
                        await outputStream.FlushAsync(cts.Token);
                        messageBuffer.SetLength(0); // Clear memory stream
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("WebSocket streaming canceled.");
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error: {ex.Message}");
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    catch { /* ignore */ }
                }

                await outputStream.DisposeAsync();
                messageBuffer.Dispose();
            }
        }
    }
}
