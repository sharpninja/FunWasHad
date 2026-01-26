using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FWH.Documentation.Sync;

/// <summary>
/// Background agent that synchronizes documentation across Functional Requirements, Technical Requirements, TODO list, and Status documents.
/// </summary>
class Program
{
    private static readonly string[] RequiredDocuments = new[]
    {
        "docs/Project/TODO.md",
        "docs/Project/Status.md",
        "docs/Project/Functional-Requirements.md",
        "docs/Project/Technical-Requirements.md"
    };

    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        
        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var config = host.Services.GetRequiredService<IConfiguration>();

        var mode = args.Length > 0 ? args[0] : "sync";
        var projectRoot = config["ProjectRoot"] ?? Directory.GetCurrentDirectory();

        logger.LogInformation("Documentation Synchronization Agent");
        logger.LogInformation("Mode: {Mode}", mode);
        logger.LogInformation("Project Root: {ProjectRoot}", projectRoot);

        try
        {
            switch (mode.ToLower())
            {
                case "check":
                    await ValidateDocumentation(projectRoot, logger);
                    break;
                case "sync":
                    await SynchronizeDocumentation(projectRoot, logger);
                    break;
                case "watch":
                    await WatchForChanges(projectRoot, logger);
                    break;
                default:
                    logger.LogError("Unknown mode: {Mode}. Use 'check', 'sync', or 'watch'", mode);
                    Environment.Exit(1);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during synchronization");
            Environment.Exit(1);
        }
    }

    private static async Task ValidateDocumentation(string projectRoot, ILogger logger)
    {
        logger.LogInformation("Validating documentation consistency...");

        var issues = new List<string>();
        var todoPath = Path.Combine(projectRoot, "docs", "Project", "TODO.md");
        var todoItems = await ParseTodoItems(todoPath);

        // Check all documents reference TODO identifiers
        foreach (var doc in RequiredDocuments)
        {
            var docPath = Path.Combine(projectRoot, doc);
            if (!File.Exists(docPath))
            {
                issues.Add($"Missing document: {doc}");
                continue;
            }

            var content = await File.ReadAllTextAsync(docPath);
            foreach (var item in todoItems)
            {
                if (!content.Contains(item.Identifier))
                {
                    issues.Add($"{doc} missing reference to {item.Identifier}");
                }
            }
        }

        if (issues.Count > 0)
        {
            logger.LogWarning("Found {Count} consistency issue(s)", issues.Count);
            foreach (var issue in issues)
            {
                logger.LogWarning("  - {Issue}", issue);
            }
            Environment.Exit(1);
        }
        else
        {
            logger.LogInformation("All documentation is consistent");
        }
    }

    private static async Task SynchronizeDocumentation(string projectRoot, ILogger logger)
    {
        logger.LogInformation("Synchronizing documentation...");

        var todoPath = Path.Combine(projectRoot, "docs", "Project", "TODO.md");
        var statusPath = Path.Combine(projectRoot, "docs", "Project", "Status.md");
        var docsPath = Path.Combine(projectRoot, "docs");

        var todoItems = await ParseTodoItems(todoPath);
        logger.LogInformation("Found {Count} TODO items", todoItems.Count);

        await UpdateStatusDocument(statusPath, todoItems, logger);
        await UpdateLastModifiedDates(projectRoot, logger);
        
        // Remove broken links
        await RemoveBrokenLinks(docsPath, logger);
        
        // Rebuild documentation
        await BuildDocumentation(docsPath, logger);

        logger.LogInformation("Synchronization complete");
    }

    private static async Task WatchForChanges(string projectRoot, ILogger logger)
    {
        logger.LogInformation("Watching for changes...");
        logger.LogInformation("Press Ctrl+C to stop");

        using var watcher = new FileSystemWatcher(projectRoot)
        {
            IncludeSubdirectories = true,
            Filter = "*.cs",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        watcher.Changed += async (sender, e) =>
        {
            logger.LogInformation("[{Time}] {ChangeType}: {Path}", 
                DateTime.Now.ToString("HH:mm:ss"), e.ChangeType, e.FullPath);
            
            await Task.Delay(2000); // Debounce
            await SynchronizeDocumentation(projectRoot, logger);
        };

        watcher.EnableRaisingEvents = true;

        // Keep running
        await Task.Delay(Timeout.Infinite);
    }

    private static async Task<List<TodoItem>> ParseTodoItems(string todoPath)
    {
        var content = await File.ReadAllTextAsync(todoPath);
        var items = new List<TodoItem>();

        // Match TODO items with identifiers: - [ ] **MVP-XXX-XXX:**
        var pattern = @"- \[([ x])\]\s+\*\*([A-Z]+-[A-Z]+-\d+):\*\*";
        var matches = Regex.Matches(content, pattern);

        foreach (Match match in matches)
        {
            var isCompleted = match.Groups[1].Value == "x";
            var identifier = match.Groups[2].Value;

            items.Add(new TodoItem
            {
                Identifier = identifier,
                IsCompleted = isCompleted
            });
        }

        return items;
    }

    private static async Task UpdateStatusDocument(string statusPath, List<TodoItem> todoItems, ILogger logger)
    {
        var content = await File.ReadAllTextAsync(statusPath);
        var updated = false;

        // Count items per project
        var stats = new Dictionary<string, ProjectStats>
        {
            { "MVP-App", new ProjectStats() },
            { "MVP-Marketing", new ProjectStats() },
            { "MVP-Support", new ProjectStats() },
            { "MVP-Legal", new ProjectStats() }
        };

        foreach (var item in todoItems)
        {
            var project = item.Identifier switch
            {
                var id when id.StartsWith("MVP-APP-") => "MVP-App",
                var id when id.StartsWith("MVP-MARKETING-") => "MVP-Marketing",
                var id when id.StartsWith("MVP-SUPPORT-") => "MVP-Support",
                var id when id.StartsWith("MVP-LEGAL-") => "MVP-Legal",
                _ => null
            };

            if (project != null && stats.ContainsKey(project))
            {
                // Determine priority from TODO.md content (would need to parse more)
                stats[project].Total++;
                if (item.IsCompleted) stats[project].Completed++;
            }
        }

        // Update last updated date
        var datePattern = @"\*Last updated: \d{4}-\d{2}-\d{2}\*";
        var newDate = $"*Last updated: {DateTime.Now:yyyy-MM-dd}*";
        if (Regex.IsMatch(content, datePattern))
        {
            content = Regex.Replace(content, datePattern, newDate);
            updated = true;
        }

        if (updated)
        {
            await File.WriteAllTextAsync(statusPath, content);
            logger.LogInformation("Updated Status.md");
        }
    }

    private static async Task UpdateLastModifiedDates(string projectRoot, ILogger logger)
    {
        var datePattern = @"\*\*Last Updated:\*\* \d{4}-\d{2}-\d{2}";
        var newDate = $"**Last Updated:** {DateTime.Now:yyyy-MM-dd}";

        foreach (var doc in new[] { "Functional-Requirements.md", "Technical-Requirements.md" })
        {
            var docPath = Path.Combine(projectRoot, "docs", "Project", doc);
            if (File.Exists(docPath))
            {
                var content = await File.ReadAllTextAsync(docPath);
                if (Regex.IsMatch(content, datePattern))
                {
                    content = Regex.Replace(content, datePattern, newDate);
                    await File.WriteAllTextAsync(docPath, content);
                    logger.LogInformation("Updated {Doc}", doc);
                }
            }
        }
    }

    private static async Task RemoveBrokenLinks(string docsPath, ILogger logger)
    {
        logger.LogInformation("Checking for broken links...");

        var markdownFiles = Directory.GetFiles(docsPath, "*.md", SearchOption.AllDirectories)
            .Where(f => !f.Contains("_site") && !f.Contains(".git"))
            .ToList();

        var totalRemoved = 0;
        var linkPattern = new Regex(@"\[([^\]]+)\]\(([^\)]+)\)");

        foreach (var filePath in markdownFiles)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var originalContent = content;
            var removedCount = 0;

            var matches = linkPattern.Matches(content);
            foreach (Match match in matches)
            {
                var linkText = match.Groups[1].Value;
                var linkPath = match.Groups[2].Value;

                // Skip external links
                if (linkPath.StartsWith("http://") || linkPath.StartsWith("https://"))
                    continue;

                // Skip anchor links
                if (linkPath.StartsWith("#"))
                    continue;

                // Resolve path
                string? resolvedPath = null;
                if (linkPath.StartsWith("~/"))
                {
                    // DocFX format: ~/ means relative to docs root
                    var relativePath = linkPath.Substring(2);
                    resolvedPath = Path.Combine(docsPath, relativePath);
                }
                else if (linkPath.StartsWith("../"))
                {
                    // Relative path: ../ means go up from current file
                    var fileDir = Path.GetDirectoryName(filePath);
                    resolvedPath = Path.GetFullPath(Path.Combine(fileDir ?? "", linkPath));
                }
                else if (!Path.IsPathRooted(linkPath))
                {
                    // Relative to current file
                    var fileDir = Path.GetDirectoryName(filePath);
                    resolvedPath = Path.GetFullPath(Path.Combine(fileDir ?? "", linkPath));
                }
                else
                {
                    resolvedPath = linkPath;
                }

                // Check if file exists
                if (resolvedPath != null && !File.Exists(resolvedPath))
                {
                    // Remove the link, keep just the text
                    content = content.Replace(match.Value, linkText);
                    removedCount++;
                    logger.LogWarning("Removed broken link: {LinkPath} from {File}", linkPath, Path.GetFileName(filePath));
                }
            }

            if (removedCount > 0)
            {
                await File.WriteAllTextAsync(filePath, content);
                totalRemoved += removedCount;
                logger.LogInformation("Removed {Count} broken link(s) from {File}", removedCount, Path.GetFileName(filePath));
            }
        }

        if (totalRemoved > 0)
        {
            logger.LogInformation("Cleaned broken links from {Count} file(s)", totalRemoved);
        }
        else
        {
            logger.LogInformation("No broken links found");
        }
    }

    private static async Task BuildDocumentation(string docsPath, ILogger logger)
    {
        logger.LogInformation("Building documentation...");

        var docfxPath = Path.Combine(docsPath, "docfx.json");
        if (!File.Exists(docfxPath))
        {
            logger.LogWarning("docfx.json not found, skipping build");
            return;
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "docfx",
                Arguments = "build docfx.json",
                WorkingDirectory = docsPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
            {
                logger.LogWarning("Could not start docfx process. Ensure docfx is installed: dotnet tool install -g docfx");
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                logger.LogInformation("Documentation built successfully");
            }
            else
            {
                logger.LogWarning("Documentation build completed with warnings/errors (exit code: {ExitCode})", process.ExitCode);
                // Log first few errors/warnings
                var lines = (output + error).Split('\n')
                    .Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase) || 
                                l.Contains("warning", StringComparison.OrdinalIgnoreCase))
                    .Take(5);
                foreach (var line in lines)
                {
                    logger.LogWarning("  {Line}", line.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building documentation");
        }
    }
}

record TodoItem
{
    public required string Identifier { get; init; }
    public bool IsCompleted { get; init; }
}

class ProjectStats
{
    public int High { get; set; }
    public int Medium { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
}
