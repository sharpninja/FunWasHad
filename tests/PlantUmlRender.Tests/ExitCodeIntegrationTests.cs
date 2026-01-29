using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace PlantUmlRender.Tests;

/// <summary>
/// Integration tests for exit codes and rendering (CR-PUM-4.5.1).
/// </summary>
public class ExitCodeIntegrationTests
{
    [Fact]
    public void NoFilesReturnsExit1()
    {
        var exit = RunRender(Array.Empty<string>());
        Assert.Equal(1, exit);
    }

    [Fact]
    public void NoStartumlPumlReturnsExit1()
    {
        using var temp = new TempDir();
        var puml = Path.Combine(temp.Dir, "nodiag.puml");
        File.WriteAllText(puml, "nothing here");

        // Use relative path since working directory is set to temp.Dir
        var exit = RunRender(new[] { "nodiag.puml" }, temp.Dir);
        Assert.Equal(1, exit);
    }

    [Fact]
    public void ValidPumlReturnsExit0AndCreatesOutput()
    {
        using var temp = new TempDir();
        var puml = Path.Combine(temp.Dir, "ok.puml");
        File.WriteAllText(puml, @"@startuml
actor a
@enduml");

        // Use relative paths since working directory is set to temp.Dir
        var exit = RunRender(new[] { "-o", ".", "-f", "svg", "ok.puml" }, temp.Dir);
        Assert.Equal(0, exit);

        var svg = Path.Combine(temp.Dir, "ok.svg");
        Assert.True(File.Exists(svg));
        Assert.True(new FileInfo(svg).Length > 0);
    }

    private static int RunRender(string[] args, string? workingDir = null)
    {
        var proj = GetRenderProjectPath();
        if (string.IsNullOrEmpty(proj) || !File.Exists(proj))
            throw new InvalidOperationException("Could not find PlantUmlRender.csproj.");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir ?? Path.GetDirectoryName(proj) ?? ".",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(proj);
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi);
        if (p == null) throw new InvalidOperationException("Could not start dotnet run.");
        
        // Read output streams to prevent deadlock
        var outputTask = p.StandardOutput.ReadToEndAsync();
        var errorTask = p.StandardError.ReadToEndAsync();
        
        if (!p.WaitForExit(90_000))
        {
            p.Kill();
            throw new InvalidOperationException("Process did not exit within timeout period.");
        }
        
        // Ensure streams are fully read
        Task.WaitAll(outputTask, errorTask);
        
        return p.ExitCode;
    }

    private static string? GetRenderProjectPath()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "tools", "PlantUmlRender", "PlantUmlRender.csproj");
            if (File.Exists(candidate)) return candidate;
            var sln = Path.Combine(dir, "FunWasHad.sln");
            if (File.Exists(sln))
            {
                candidate = Path.Combine(dir, "tools", "PlantUmlRender", "PlantUmlRender.csproj");
                if (File.Exists(candidate)) return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private sealed class TempDir : IDisposable
    {
        public string Dir { get; }

        public TempDir()
        {
            Dir = Path.Combine(Path.GetTempPath(), "PlantUmlRender.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(Dir)) Directory.Delete(Dir, recursive: true); } catch { }
        }
    }
}
