using System.Diagnostics;

public static class CommandLine
{
    public static async Task<CommandResult> RunAsync(string workingDirectory, string fileName, params string[] segments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var segment in segments)
            startInfo.ArgumentList.Add(segment);

        using var process = Process.Start(startInfo)!;

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var result = new CommandResult(stdout, stderr, process.ExitCode);

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Command ({string.Join(" ", segments)}) failed with exit code {result.ExitCode}\n\nStandard output: {result.StandardOutput}\n\nStandard error: {result.StandardError}");

        return result;
    }
}

public record CommandResult(string StandardOutput, string StandardError, int ExitCode);