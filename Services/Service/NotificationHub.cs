using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Services.Service
{
    public class NotificationHub : Hub
    {
        // Mapping UserId => Tập connectionIds để gửi message đúng tất cả client của user
        private static readonly ConcurrentDictionary<Guid, HashSet<string>> UserConnections = new();

        // Khi client connect, lấy userId từ query string và thêm connectionId vào tập của userId
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.Request.Query["userId"].FirstOrDefault();

            if (Guid.TryParse(userIdStr, out var userId))
            {
                UserConnections.AddOrUpdate(userId,
                    id => new HashSet<string> { Context.ConnectionId },  // Nếu chưa có thì tạo mới tập connectionId
                    (id, existingSet) =>
                    {
                        lock (existingSet)
                        {
                            existingSet.Add(Context.ConnectionId);
                        }
                        return existingSet;
                    });
            }

            return base.OnConnectedAsync();
        }

        // Khi client disconnect, xóa connectionId khỏi tập connectionIds của userId, nếu tập rỗng thì xóa userId
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var item = UserConnections.FirstOrDefault(kvp => kvp.Value.Contains(Context.ConnectionId));
            if (item.Key != Guid.Empty)
            {
                lock (item.Value)
                {
                    item.Value.Remove(Context.ConnectionId);
                    if (item.Value.Count == 0)
                    {
                        UserConnections.TryRemove(item.Key, out _);
                    }
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        // Lấy tất cả connectionId theo userId
        public static HashSet<string> GetConnectionIdsStatic(Guid userId)
        {
            return UserConnections.TryGetValue(userId, out var connections) ? connections : null;
        }


        // Gửi yêu cầu chia sẻ màn hình cho student, gửi tới tất cả các connectionId của student
        public async Task SendRequestToStudent(Guid studentId, string message)
        {
            if (UserConnections.TryGetValue(studentId, out var connections))
            {
                foreach (var connId in connections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveRequestShare", message);
                }
            }
        }

        // Gửi URL stream cho teacher (nên mở rộng truyền teacherId nếu có), gửi tới tất cả connectionId của teacher
        public async Task SendStreamUrlToTeacher(Guid teacherId, object streamInfo)
        {
            if (UserConnections.TryGetValue(teacherId, out var connections))
            {
                foreach (var connId in connections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveStreamUrl", streamInfo);
                }
            }
        }
    }
}
