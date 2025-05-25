using System.Diagnostics;

namespace DoAnTotNghiep.Services.Streaming
{
    public class FFmpegService
    {
        public Process StartHlsProcess(Guid studentId, Stream inputStream, CancellationToken cancelToken)
        {
            string outputDirectory = Path.Combine("HLSStreams", studentId.ToString());
            Directory.CreateDirectory(outputDirectory);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i pipe:0 " +
                                "-c:v libx264 -profile:v baseline -level 3.0 " +
                                "-preset veryfast -crf 23 -g 30 -sc_threshold 0 " +
                                "-f hls -hls_time 2 -hls_list_size 5 " +
                                "-hls_flags delete_segments+append_list " +
                                $"{Path.Combine(outputDirectory, "stream.m3u8")}",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.WriteLine($"FFmpeg: {e.Data}");
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            // Cancel nếu token bị hủy
            cancelToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch { }
            });

            // Copy input stream sang FFmpeg
            _ = Task.Run(async () =>
            {
                try
                {
                    await inputStream.CopyToAsync(process.StandardInput.BaseStream, cancelToken);
                    process.StandardInput.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Streaming error: {ex.Message}");
                }
            }, cancelToken);

            return process;
        }
    }
}
