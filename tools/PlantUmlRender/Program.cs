using System.Runtime.CompilerServices;
using PlantUml.Net;

[assembly: InternalsVisibleTo("PlantUmlRender.Tests")]

namespace PlantUmlRender;

/// <summary>Renders PlantUML .puml files to PNG and/or SVG. CR-PUM-4.1.2: Only render trusted .puml files. PlantUml.Net uses a JVM/PlantUML; malicious .puml could in theory affect the JVM. -o, -f, and positional args are parsed by hand; CR-PUM-4.4.1: System.CommandLine could be used for -o, -f, --help. PlantUmlSettings() uses library defaults; CR-PUM-4.4.3: add -s/--server when a custom PlantUML server is needed.</summary>
static class Program
{
    /// <summary>CR-PUM-4.1.1: Base directory for path validation. outputDir and input files must be under this. Internal for tests (CR-PUM-4.5.1).</summary>
    internal static bool IsUnderBase(string fullPath, string baseDir)
    {
        var normalized = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var baseNorm = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalized.StartsWith(baseNorm, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Parse -o, -f, and positional files. Internal for tests (CR-PUM-4.5.1). Returns unknownFormatValue when -f gets an unknown value.</summary>
    internal static (string outputDir, bool wantSvg, bool wantPng, List<string> files, string? unknownFormatValue) ParseArgs(string[] args)
    {
        var outputDir = ".";
        var wantSvg = true;
        var wantPng = true;
        var files = new List<string>();
        string? unknownFormatValue = null;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if ((a == "-o" || a == "--output") && i + 1 < args.Length)
            {
                outputDir = args[++i];
            }
            else if ((a == "-f" || a == "--formats") && i + 1 < args.Length)
            {
                var f = args[++i].ToLowerInvariant();
                if (f == "svg") { wantSvg = true; wantPng = false; }
                else if (f == "png") { wantSvg = false; wantPng = true; }
                else if (f == "both") { wantSvg = true; wantPng = true; }
                else { wantSvg = true; wantPng = true; unknownFormatValue = f; }
            }
            else if (!a.StartsWith('-'))
            {
                files.Add(a);
            }
        }

        return (outputDir, wantSvg, wantPng, files, unknownFormatValue);
    }

    /// <summary>Extract @startuml..@enduml block from text. Returns null if not found. Internal for tests (CR-PUM-4.5.1).</summary>
    internal static string? ExtractStartumlBlock(string text)
    {
        var si = text.IndexOf("@startuml", StringComparison.OrdinalIgnoreCase);
        var ei = text.IndexOf("@enduml", StringComparison.OrdinalIgnoreCase);
        if (si < 0 || ei <= si) return null;
        return text[si..(ei + "@enduml".Length)];
    }

    static async Task<int> Main(string[] args)
    {
        var baseDir = Path.GetFullPath(Environment.CurrentDirectory);
        var (outputDirStr, wantSvg, wantPng, files, unknownFormatValue) = ParseArgs(args);

        if (unknownFormatValue != null)
            Console.Error.WriteLine($"Unknown format: {unknownFormatValue}; using both svg and png.");

        if (files.Count == 0)
        {
            Console.Error.WriteLine("Usage: plantuml-render [-o <out-dir>] [-f svg|png|both] <file.puml> [file2.puml ...]");
            return 1;
        }

        var outputDir = new DirectoryInfo(outputDirStr);

        // CR-PUM-4.1.1: validate outputDir and input files are under base (CurrentDirectory)
        var outputFull = Path.GetFullPath(outputDir.FullName);
        if (!IsUnderBase(outputFull, baseDir))
        {
            Console.Error.WriteLine($"Output directory is outside the current directory: {outputFull}");
            return 1;
        }
        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            var fileFull = Path.GetFullPath(fi.FullName);
            if (!IsUnderBase(fileFull, baseDir))
            {
                Console.Error.WriteLine($"Input file is outside the current directory: {fileFull}");
                return 1;
            }
        }

        var failedCount = await RenderAll(outputDir, wantSvg, wantPng, files.Select(f => new FileInfo(f)).ToArray(), CancellationToken.None).ConfigureAwait(false);
        return failedCount > 0 ? 1 : 0;
    }

    /// <summary>CR-PUM-4.2.1: returns failedCount (files that had no valid diagram or render errors).</summary>
    static async Task<int> RenderAll(DirectoryInfo outputDir, bool wantSvg, bool wantPng, FileInfo[] inputs, CancellationToken ct)
    {
        // CR-PUM-4.2.3: wrap Create() for UnauthorizedAccessException/IOException
        try
        {
            outputDir.Create();
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Cannot create output directory: {ex.Message}");
            return inputs.Length;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Cannot create output directory: {ex.Message}");
            return inputs.Length;
        }

        var factory = new RendererFactory();
        var renderer = factory.CreateRenderer(new PlantUmlSettings()); // CR-PUM-4.4.3: PlantUmlSettings() uses library defaults; -s/--server when custom server needed.
        // CR-PUM-4.3.1: parallel rendering with MaxDegreeOfParallelism 4. Assumes PlantUml.Net renderer is thread-safe for concurrent RenderAsync calls.
        var parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct };
        var failed = new int[1];

        await Parallel.ForEachAsync(inputs, parallelOpts, async (file, cti) =>
        {
            if (!file.Exists)
            {
                Console.Error.WriteLine($"File not found: {file.FullName}");
                Interlocked.Increment(ref failed[0]);
                return;
            }
            if (!string.Equals(file.Extension, ".puml", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Skipping non-.puml file: {file.FullName}");
                return;
            }

            var text = await File.ReadAllTextAsync(file.FullName, cti).ConfigureAwait(false);
            var block = ExtractStartumlBlock(text);
            if (block == null)
            {
                Console.Error.WriteLine($"No valid @startuml/@enduml block in {file.Name}; skipping.");
                Interlocked.Increment(ref failed[0]);
                return;
            }
            text = block;

            var baseName = Path.GetFileNameWithoutExtension(file.Name);
            var producedAny = false;

            if (wantPng) producedAny |= await RenderOne(file, text, OutputFormat.Png, baseName, outputDir, (t, f, c) => renderer.RenderAsync(t, f, c), cti).ConfigureAwait(false);
            if (wantSvg) producedAny |= await RenderOne(file, text, OutputFormat.Svg, baseName, outputDir, (t, f, c) => renderer.RenderAsync(t, f, c), cti).ConfigureAwait(false);

            if ((wantPng || wantSvg) && !producedAny)
                Interlocked.Increment(ref failed[0]);
        }).ConfigureAwait(false);
        return failed[0];
    }

    /// <summary>CR-PUM-4.4.2: Renders text to one format and writes to outputDir. Returns true if output was written.</summary>
    static async Task<bool> RenderOne(FileInfo file, string text, OutputFormat format, string baseName, DirectoryInfo outputDir, Func<string, OutputFormat, CancellationToken, Task<byte[]>> renderAsync, CancellationToken ct)
    {
        try
        {
            var bytes = await renderAsync(text, format, ct).ConfigureAwait(false);
            if (bytes is not { Length: > 0 })
            {
                Console.Error.WriteLine($"Empty {format} for {file.Name}");
                return false;
            }
            var ext = format == OutputFormat.Png ? ".png" : ".svg";
            var outPath = Path.Combine(outputDir.FullName, baseName + ext);
            await File.WriteAllBytesAsync(outPath, bytes, ct).ConfigureAwait(false);
            Console.WriteLine($"Rendered: {outPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to render {format} for {file.Name}: {ex.Message}");
            return false;
        }
    }
}
