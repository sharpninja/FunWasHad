using FWH.Common.Imaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceTests
{
    /// <summary>
    /// Tests that RenderSvgOverlay correctly composites an SVG overlay onto a base bitmap at the specified pixel coordinates.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's ability to render an SVG overlay onto a base bitmap at specific coordinates.</para>
    /// <para><strong>Data involved:</strong> A 100x100 pixel red base bitmap (RGB 255,0,0) and a 10x10 pixel blue SVG square (RGB 0,0,255) positioned at coordinates (20.5, 30.25). The fractional coordinates test that the service correctly rounds to pixel boundaries.</para>
    /// <para><strong>Why the data matters:</strong> SVG overlays are used to add annotations, markers, or UI elements to images (e.g., location markers on maps, badges on photos). The overlay must be positioned accurately at the specified coordinates, and fractional coordinates must be handled correctly (rounded to nearest pixel). The color verification ensures the overlay is rendered correctly and doesn't affect pixels outside the overlay area.</para>
    /// <para><strong>Expected outcome:</strong> The pixel at (20, 30) - the rounded overlay origin - should be blue (RGB 0,0,255), and the pixel at (5, 5) - outside the overlay - should remain red (RGB 255,0,0).</para>
    /// <para><strong>Reason for expectation:</strong> The RenderSvgOverlay method should render the SVG at the specified coordinates, rounding fractional coordinates to the nearest pixel. Pixels within the overlay area (20-29, 30-39) should be blue, while pixels outside should retain the base bitmap color (red). The specific pixel checks confirm accurate positioning and that the overlay doesn't affect unrelated areas of the image.</para>
    /// </remarks>
    [Fact]
    public void RenderSvgOverlayCompositesSvgOverBaseBitmapAtCorrectPixel()
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
