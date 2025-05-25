namespace DoAnTotNghiep.Services.Streaming
{
    public class FileCleanUpService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupOldStreams();
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup error: {ex.Message}");
                }
            }
        }

        private void CleanupOldStreams()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "HLSStreams");

            if (!Directory.Exists(baseDir)) return;

            foreach (var studentDir in Directory.GetDirectories(baseDir))
            {
                var lastWrite = Directory.GetLastWriteTime(studentDir);
                if (DateTime.Now - lastWrite > TimeSpan.FromMinutes(30))
                {
                    try
                    {
                        Directory.Delete(studentDir, true);
                        Console.WriteLine($"Deleted old stream: {studentDir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting {studentDir}: {ex.Message}");
                    }
                }
            }
        }
    }
}
