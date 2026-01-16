using Xunit;
using SkiaSharp;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Imaging;
using FWH.Common.Imaging.Extensions;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceTests
{
    [Fact]
    public void RenderSvgOverlay_CompositesSvgOverBaseBitmap_AtCorrectPixel()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();

        var svc = sp.GetRequiredService<IImagingService>();

        // Create a 100x100 red base bitmap
        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 0, 0));
        }

        // Simple SVG: small blue square 10x10
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\">" +
                  "<rect width=\"10\" height=\"10\" fill=\"#0000FF\" />" +
                  "</svg>";

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 20.5f, y: 30.25f);

        // Assert: pixel at overlay origin rounded to nearest integer should be blue
        var colorAtOrigin = result.GetPixel(20, 30);
        Assert.Equal(0, colorAtOrigin.Red);
        Assert.Equal(0, colorAtOrigin.Green);
        Assert.Equal(255, colorAtOrigin.Blue);

        // Assert: pixel outside overlay remains red
        var colorOutside = result.GetPixel(5, 5);
        Assert.Equal(255, colorOutside.Red);
        Assert.Equal(0, colorOutside.Green);
        Assert.Equal(0, colorOutside.Blue);
    }
}
