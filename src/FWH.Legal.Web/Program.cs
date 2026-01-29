using MDS.AspnetServices;

var builder = WebApplication.CreateBuilder(args);

builder.AddMarkdownServer();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// MarkdownServer must run before StaticFiles so *.md requests are rendered as HTML
// instead of being served raw from wwwroot. Non-.md paths (e.g. /css/site.css) fall
// through to StaticFiles.
app.UseMarkdownServer();
app.UseStaticFiles();

// Health endpoint for Docker, Aspire, and load balancers
app.MapGet("/health", () => Results.Ok());

app.Run();
