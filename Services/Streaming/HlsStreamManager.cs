using System.Collections.Concurrent;
using System.Diagnostics;

namespace DoAnTotNghiep.Services.Streaming
{
    public class HlsStreamManager
    {
        private readonly ConcurrentDictionary<Guid, Process> activeStreams = new();
        private readonly FFmpegService ffmpegService;

        public HlsStreamManager(FFmpegService ffmpegService)
        {
            this.ffmpegService = ffmpegService;
        }

        public async Task StartStreamAsync(Guid studentId, Stream inputStream, CancellationToken cancelToken)
        {
            var process = ffmpegService.StartHlsProcess(studentId, inputStream, cancelToken);

            if (!activeStreams.TryAdd(studentId, process))
            {
                process.Kill(); // dừng ngay nếu không dùng được
                throw new InvalidOperationException("Stream already exists for this student.");
            }

            await Task.Run(() =>
            {
                try
                {
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FFmpeg process error: {ex.Message}");
                }
                finally
                {
                    activeStreams.TryRemove(studentId, out _);

                    string dir = Path.Combine("HLSStreams", studentId.ToString());
                    if (Directory.Exists(dir))
                    {
                        try
                        {
                            Directory.Delete(dir, true); // Chỉ xóa nếu bạn không cần lưu
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete stream folder: {ex.Message}");
                        }
                    }
                }
            }, cancelToken);
        }

        public string? GetStreamPath(Guid studentId)
        {
            string path = Path.Combine("HLSStreams", studentId.ToString(), "stream.m3u8");
            return File.Exists(path) ? path : null;
        }

        public bool StopStream(Guid studentId)
        {
            if (activeStreams.TryRemove(studentId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping FFmpeg process: {ex.Message}");
                }
            }
            return false;
        }
    }
}
