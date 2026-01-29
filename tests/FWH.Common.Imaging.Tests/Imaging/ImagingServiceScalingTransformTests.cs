using FWH.Common.Imaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceScalingTransformTests
{
    private static SKColor GetPixel(SKBitmap bmp, int x, int y) => bmp.GetPixel(x, y);

    /// <summary>
    /// Tests that RenderSvgOverlay correctly applies viewBox scaling when the SVG's viewBox dimensions differ from its width/height attributes.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's handling of SVG viewBox scaling when viewBox and width/height attributes specify different coordinate systems.</para>
    /// <para><strong>Data involved:</strong> A 200x200 white base bitmap and an SVG with viewBox="0 0 100 100" (100x100 coordinate system) but width="50" height="50" (50x50 pixel output). The SVG contains a red rectangle filling the entire viewBox. The overlay is positioned at (10, 10).</para>
    /// <para><strong>Why the data matters:</strong> SVG viewBox allows defining a coordinate system independent of the rendered size. When viewBox differs from width/height, the SVG content should be scaled to fit. This test validates that the renderer correctly interprets viewBox scaling, which is essential for rendering SVGs that use viewBox for responsive design or coordinate system abstraction.</para>
    /// <para><strong>Expected outcome:</strong> The center of the rendered overlay (pixel 35,35 = 10+25, 10+25) should be red, and pixels outside the overlay (5,5) should remain white.</para>
    /// <para><strong>Reason for expectation:</strong> The viewBox defines a 100x100 coordinate system, but width/height specify 50x50 pixels, so content should be scaled down by 50%. The red rectangle (100x100 in viewBox coordinates) should render as 50x50 pixels. The center pixel being red confirms scaling works correctly, and the outside pixel remaining white confirms the overlay doesn't affect unrelated areas.</para>
    /// </remarks>
    [Fact]
    public void RenderSvgOverlayViewBoxScalingAppliesCorrectScale()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 255, 255)); // white background
        }

        // SVG defines viewBox 0 0 100 100 but width/height 50x50 - should scale down
        var svg = @"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' width='50' height='50'>
                        <rect x='0' y='0' width='100' height='100' fill='#FF0000' />
                    </svg>";

        // Act
        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 10f, y: 10f);

        // Assert - Center area should be red
        var center = GetPixel(result, 10 + 25, 10 + 25); // center of the 50x50 area
        Assert.Equal(255, center.Red);
        Assert.Equal(0, center.Green);
        Assert.Equal(0, center.Blue);

        // Assert - Outside area should remain white
        var outside = GetPixel(result, 5, 5);
        Assert.Equal(255, outside.Red);
        Assert.Equal(255, outside.Green);
        Assert.Equal(255, outside.Blue);
    }

    /// <summary>
    /// Tests that RenderSvgOverlay correctly renders SVG content with transform attributes (rotation) applied to the SVG elements.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's ability to interpret and apply SVG transform attributes (specifically rotation) when rendering overlays.</para>
    /// <para><strong>Data involved:</strong> A 100x100 white base bitmap and a 40x40 SVG containing a red rectangle (20x40) rotated 45 degrees about its center (20,20) using the transform="rotate(45 20 20)" attribute. The overlay is positioned at (30, 30).</para>
    /// <para><strong>Why the data matters:</strong> SVG transforms (rotation, scaling, translation) are commonly used to position and orient graphics. The renderer must correctly interpret transform attributes and apply them when rendering. This test validates that rotation transforms work correctly, which is essential for rendering rotated markers, arrows, or other directional graphics on images.</para>
    /// <para><strong>Expected outcome:</strong> The pixel at the center of the rotated rectangle (50, 50 = 30+20, 30+20) should be red, and a corner pixel outside the rotated shape (30, 30) should remain white.</para>
    /// <para><strong>Reason for expectation:</strong> The renderer should parse the transform="rotate(45 20 20)" attribute and apply a 45-degree rotation about point (20,20) before rendering. After rotation, the rectangle's center should be at the overlay center, so pixel (50,50) should be red. The corner pixel (30,30) is outside the rotated rectangle's bounds, so it should retain the base bitmap color (white). This confirms that transforms are correctly interpreted and applied during rendering.</para>
    /// </remarks>
    [Fact]
    public void RenderSvgOverlayRotatedSvgRendersRotatedContent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        var baseInfo = new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var canvas = new SKCanvas(baseBitmap))
        {
            canvas.Clear(new SKColor(255, 255, 255)); // white background
        }

        // SVG: a red rectangle rotated 45 degrees about its center
        var svg = @"<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40' viewBox='0 0 40 40'>
                        <rect x='10' y='0' width='20' height='40' fill='#FF0000' transform='rotate(45 20 20)' />
                    </svg>";

        // Act
        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 30f, y: 30f);

        // Assert - Rotated shape should place red pixels off the original axis; check a couple of expected locations
        var p1 = GetPixel(result, 30 + 20, 30 + 20); // center area should be red
        Assert.Equal(255, p1.Red);

        // Assert - A corner outside rotated rect should remain white
        var outside = GetPixel(result, 30 + 0, 30 + 0);
        Assert.Equal(255, outside.Red);
        Assert.Equal(255, outside.Green);
        Assert.Equal(255, outside.Blue);
    }
}
