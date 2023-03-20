using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.FFMPEG;

public class WaveformEngine
{
    public WaveformEngine(string inputPath, string outputPath, int streamIndex, Color? color = null)
    {
        ExecutablePath = Path.Combine(App.LibPath, "ffmpeg.exe");
        ;
        InputPath   = inputPath;
        OutputPath  = outputPath;
        StreamIndex = streamIndex;
        Color       = color ?? Color.LightGray;
    }

    #region Properties

    /// <summary>
    ///     Full path to ffmpeg.exe
    /// </summary>
    private string ExecutablePath { get; }

    /// <summary>
    ///     Input media path
    /// </summary>
    private string InputPath { get; }

    /// <summary>
    ///     Output media path
    /// </summary>
    private string OutputPath { get; }

    /// <summary>
    ///     Video stream index
    /// </summary>
    private int StreamIndex { get; }

    /// <summary>
    ///     Resolution of the waveform in the X dimension.
    /// </summary>
    public static int ResolutionX => 8192;

    /// <summary>
    ///     Resolution of the waveform in the Y dimension.
    /// </summary>
    private static int ResolutionY => 64;

    /// <summary>
    ///     Color of the waveform
    /// </summary>
    private Color Color { get; }

    /// <summary>
    ///     Output from ffmpeg
    /// </summary>
    private StringBuilder Output { get; } = new();

    #endregion

    #region Methods

    /// <summary>
    ///     Runs the waveform engine process.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    public async Task<int> Run()
    {
        var argumentStr = $"-y -i \"{InputPath}\" -filter_complex \"[0:a:{StreamIndex}]aformat=channel_layouts=mono,compand,showwavespic=s={ResolutionX}x{ResolutionY}:" +
                          $"colors=#{Color.R:X2}{Color.G:X2}{Color.B:X2}{Color.A:X2}\" -frames:v 1 \"{OutputPath}\"";
        Output.AppendLine($"ffmpeg.exe {argumentStr}");

        var process = new Process
        {
            StartInfo = new()
            {
                FileName               = ExecutablePath,
                Arguments              = argumentStr,
                CreateNoWindow         = true,
                UseShellExecute        = false,
                RedirectStandardError  = true,
                RedirectStandardOutput = true
            }
        };

        process.ErrorDataReceived  += OnProcessOutput;
        process.OutputDataReceived += OnProcessOutput;
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        try
        {
            await process.WaitForExitAsync();
        }
        catch (TaskCanceledException e)
        {
            throw e;
        }

        return process.ExitCode;
    }


    private void OnProcessOutput(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;

        Output.AppendLine(e.Data);
    }

    #endregion
}