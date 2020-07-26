using System;
using System.IO;

namespace auxmic.ui
{
    /// <summary>
    /// FFmpeg wrapper
    /// </summary>
    internal class FFmpegTool
    {
        /// <summary>
        /// Full path to FFmpeg executable file `ffmpeg.exe`
        /// </summary>
        public string FFmpegExe { get; }

        /// <summary>
        /// Initialize path to FFmpeg executable
        /// </summary>
        /// <param name="pathToFFmpegExe">Full path to FFmpeg executable file `ffmpeg.exe`</param>
        public FFmpegTool(string pathToFFmpegExe)
        {
            FFmpegExe = pathToFFmpegExe;
        }

        /// <summary>
        /// Exports shortest synced file.
        /// </summary>
        /// <param name="videoFilePath">Source video file path</param>
        /// <param name="audioFilePath">Audio file path</param>
        /// <param name="offset">Audio offset</param>
        /// <param name="targetFilePath">Target synced file path</param>
        internal void Export(string videoFilePath, string audioFilePath, TimeSpan offset, string targetFilePath)
        {
            if (!File.Exists(this.FFmpegExe))
            {
                new ApplicationException("Full path to FFmpeg executable file `ffmpeg.exe` is not set or not correct.");
            }

            string offsetInvariantCulture = offset.ToString("g", System.Globalization.CultureInfo.InvariantCulture);

            // command template to export shortest synced video
            // check https://ffmpeg.org/ffmpeg.html for params:
            // -ss position(input / output)
            string exportArgs = $"-i \"{videoFilePath}\" -ss {offsetInvariantCulture} -i \"{audioFilePath}\" -c copy -map 0:v:0 -map 1:a:0 -shortest \"{targetFilePath}\"";

            System.Diagnostics.Process.Start($"\"{FFmpegExe}\"", exportArgs);
        }
    }
}
