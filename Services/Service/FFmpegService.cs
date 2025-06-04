using DoAnTotNghiep.Dto.Streaming;
using DoAnTotNghiep.Services.IService;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DoAnTotNghiep.Services.ServiceImplement
{
    public class FFmpegService : IFFmpegService
    {
        // Xây dựng args chạy ffmpeg (đầu vào pipe, đầu ra HLS)
        public string BuildArguments(string inputPipePath, Guid streamId)
        {
            string outputFolder = Path.Combine("wwwroot", "live", streamId.ToString());
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string outputPath = Path.Combine(outputFolder, "playlist.m3u8");

            // Tối ưu cấu hình hls để giảm delay, tăng tương thích
            return $"-f webm -i {inputPipePath} " +
                   "-c:v libx264 " +
                   "-preset veryfast " + // Tối ưu preset cho tốc độ
                   "-pix_fmt yuv420p " +
                   "-g 50 " + // Số lượng keyframe phù hợp (giảm độ trễ)
                   "-sc_threshold 0 " +
                   "-f hls " +
                   "-hls_time 2 " + // Giảm thời gian mỗi segment để giảm độ trễ
                   "-hls_list_size 5 " +
                   "-hls_flags delete_segments+omit_endlist " +
                   "-max_muxing_queue_size 1024 " + // Tăng kích thước queue để tránh lỗi buffer
                   "-bufsize 1M " + // Giới hạn bộ đệm để giảm độ trễ
                   "-b:v 1500k " + // Giảm bitrate video để giảm tải băng thông
                   $"-hls_segment_type mpegts " + // Đảm bảo rằng đoạn video sử dụng mpegts cho độ tương thích tốt hơn
                   $"\"{outputPath}\"";
        }


        // Tạo Named Pipe trước khi start FFmpeg
        public async Task<NamedPipeServerStream> CreateInputPipeAsync(string pipePath)
        {
            var pipeName = pipePath.Replace(@"\\.\pipe\", "");
            var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough // đảm bảo dữ liệu flush nhanh
            );

            Console.WriteLine($"Created pipe: {pipePath}");

            // Thêm timeout cho WaitForConnectionAsync, tránh block vô thời hạn
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await pipeServer.WaitForConnectionAsync(cts.Token);
                Console.WriteLine("Pipe connected.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Timeout waiting for pipe connection.");
                pipeServer.Dispose();
                throw;
            }

            return pipeServer;
        }


        // Start process ffmpeg với pipe đã được tạo
        public async Task<FFmpegProcessInfo> StartFFmpegAsync(string inputPipePath, Guid streamId)
        {
            var args = BuildArguments(inputPipePath, streamId);
            Console.WriteLine($"FFmpeg command: ffmpeg {args}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                }
            };

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Không thể khởi động quá trình FFmpeg");
                }

                // Logging và giám sát quá trình
                LogProcessOutput(process, streamId);
                MonitorProcessExit(process, streamId);

                // Đợi FFmpeg khởi động
                await Task.Delay(500);
                return new FFmpegProcessInfo
                {
                    ProcessId = process.Id,
                    InputPipePath = inputPipePath,
                    Process = process
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi khởi động FFmpeg cho stream {streamId}: {ex.Message}");
                process?.Dispose();
                throw;
            }
        }

        private void LogProcessOutput(Process process, Guid streamId)
        {
            Task.Run(async () =>
            {
                var errorReader = process.StandardError;
                while (!errorReader.EndOfStream)
                {
                    var line = await errorReader.ReadLineAsync();
                    LogFFmpegOutput(line, streamId);
                }
            });
        }

        private void MonitorProcessExit(Process process, Guid streamId)
        {
            Task.Run(async () =>
            {
                await process.WaitForExitAsync();
                Console.WriteLine($"Quá trình FFmpeg {process.Id} hoàn thành cho stream {streamId} với mã thoát: {process.ExitCode}");
            });
        }

        private void LogFFmpegOutput(string line, Guid streamId)
        {
            if (line.Contains("error"))
            {
                Console.WriteLine($"FFmpeg LỖI [{streamId}]: {line}");
            }
            else if (line.Contains("warning"))
            {
                Console.WriteLine($"FFmpeg CẢNH BÁO [{streamId}]: {line}");
            }
            else
            {
                Console.WriteLine($"FFmpeg THÔNG TIN [{streamId}]: {line}");
            }
        }


        public async Task<bool> StopFFmpegAsync(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process != null && !process.HasExited)
                {
                    // Dùng Kill process tree nếu cần để chắc chắn kill hết child process
                    Console.WriteLine($"Stopping FFmpeg process {processId}...");

                    // Thử close nhẹ nhàng bằng Kill
                    process.Kill(entireProcessTree: true);

                    await process.WaitForExitAsync();
                    Console.WriteLine("FFmpeg stopped.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping FFmpeg process {processId}: {ex.Message}");
                return false;
            }
        }

        // Lấy stream pipe ghi dữ liệu cho ffmpeg (không sử dụng nữa - thay bằng NamedPipeServerStream)
        public Stream GetFFmpegInputPipeStream(string pipePath)
        {
            return new FileStream(pipePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        }
    }
}