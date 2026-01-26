using FWH.Common.Imaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace FWH.Common.Imaging.Tests.Imaging;

public class ImagingServiceFitAnchorTests
{
    public static IEnumerable<object[]> FitAnchorData()
    {
        foreach (FitMode fm in Enum.GetValues(typeof(FitMode)))
        {
            foreach (Anchor a in Enum.GetValues(typeof(Anchor)))
            {
                yield return new object[] { fm, a };
            }
        }
    }

    /// <summary>
    /// Tests that RenderSvgOverlay correctly applies FitMode and Anchor options to scale and position SVG overlays according to the specified fitting and anchoring behavior.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's handling of ImagingOptions with different FitMode (BestFit, Fill, Stretch) and Anchor (Center, TopLeft, TopRight, BottomLeft, BottomRight) combinations.</para>
    /// <para><strong>Data involved:</strong> A 100x100 red base bitmap and a 20x10 blue SVG rectangle. The test uses all combinations of FitMode and Anchor enums (15 total combinations). Each combination should scale and position the overlay differently: BestFit maintains aspect ratio, Fill fills the area (may crop), Stretch distorts to fill, and Anchor determines placement (center, corners).</para>
    /// <para><strong>Why the data matters:</strong> FitMode and Anchor options allow flexible overlay positioning and sizing for different use cases (e.g., centered watermarks, corner badges, full-screen overlays). The renderer must correctly interpret these options and apply the appropriate scaling and positioning. This test validates all option combinations work correctly.</para>
    /// <para><strong>Expected outcome:</strong> For each FitMode/Anchor combination, the overlay should be rendered such that: (1) the overlay intersects the output (is visible), (2) a pixel at the calculated center of the overlay is blue, and (3) a pixel at the calculated top-left corner (based on anchor) is blue.</para>
    /// <para><strong>Reason for expectation:</strong> The renderer should calculate the desired size based on FitMode (scaling the 20x10 SVG to fit the 100x100 area), then calculate the offset based on Anchor (positioning the scaled overlay). The blue pixels at the calculated positions confirm the scaling and positioning calculations are correct. The intersection check ensures the overlay is actually rendered (not positioned completely outside the image).</para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(FitAnchorData))]
    public void RenderSvgOverlayFitModeAnchorComposesOverlay(FitMode fitMode, Anchor anchor)
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        const int baseW = 100;
        const int baseH = 100;

        // Create base red bitmap
        var baseInfo = new SKImageInfo(baseW, baseH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var c = new SKCanvas(baseBitmap)) c.Clear(new SKColor(255, 0, 0));

        // Simple SVG: rectangle 20x10 filled blue
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\">" +
                  "<rect x=\"0\" y=\"0\" width=\"20\" height=\"10\" fill=\"#0000FF\" />" +
                  "</svg>";

        var options = new ImagingOptions { FitMode = fitMode, Anchor = anchor, Padding = 0 };

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 0, y: 0, options: options);

        // Compute expected placement using same logic as ImagingService
        var srcW = 20f; var srcH = 10f;
        var paddedW = baseW - options.Padding * 2;
        var paddedH = baseH - options.Padding * 2;

        float desiredWf = srcW, desiredHf = srcH;
        switch (options.FitMode)
        {
            case FitMode.BestFit:
                {
                    var scaleX = paddedW / srcW;
                    var scaleY = paddedH / srcH;
                    var scale = Math.Min(scaleX, scaleY);
                    desiredWf = srcW * scale; desiredHf = srcH * scale; break;
                }
            case FitMode.Fill:
                {
                    var scaleX = paddedW / srcW;
                    var scaleY = paddedH / srcH;
                    var scale = Math.Max(scaleX, scaleY);
                    desiredWf = srcW * scale; desiredHf = srcH * scale; break;
                }
            case FitMode.Stretch:
                desiredWf = paddedW; desiredHf = paddedH; break;
        }

        var desiredW = Math.Ceiling(desiredWf);
        var desiredH = Math.Ceiling(desiredHf);

        float offsetX = 0f, offsetY = 0f;
        switch (options.Anchor)
        {
            case Anchor.Center:
                offsetX = options.Padding + (float)((paddedW - desiredW) / 2.0);
                offsetY = options.Padding + (float)((paddedH - desiredH) / 2.0);
                break;
            case Anchor.TopLeft:
                offsetX = options.Padding; offsetY = options.Padding; break;
            case Anchor.TopRight:
                offsetX = options.Padding + (float)(paddedW - desiredW); offsetY = options.Padding; break;
            case Anchor.BottomLeft:
                offsetX = options.Padding; offsetY = options.Padding + (float)(paddedH - desiredH); break;
            case Anchor.BottomRight:
                offsetX = options.Padding + (float)(paddedW - desiredW); offsetY = options.Padding + (float)(paddedH - desiredH); break;
        }

        // Intersection with output
        var interLeft = Math.Max(0, offsetX);
        var interTop = Math.Max(0, offsetY);
        var interRight = Math.Min(baseW, offsetX + (float)desiredW);
        var interBottom = Math.Min(baseH, offsetY + (float)desiredH);

        Assert.True(interRight > interLeft && interBottom > interTop, "Overlay did not intersect output (unexpected)");

        var sampleX = (int)Math.Round((interLeft + interRight) / 2.0);
        var sampleY = (int)Math.Round((interTop + interBottom) / 2.0);
        sampleX = Math.Clamp(sampleX, 0, baseW - 1);
        sampleY = Math.Clamp(sampleY, 0, baseH - 1);

        var color = result.GetPixel(sampleX, sampleY);

        // Expect blue at the sampled point
        Assert.Equal(0, color.Red);
        Assert.Equal(0, color.Green);
        Assert.Equal(255, color.Blue);

        // Additionally assert the overlay top-left aligns exactly for each anchor (rounded to nearest pixel)
        var expectedTopLeftX = (int)Math.Round(offsetX);
        var expectedTopLeftY = (int)Math.Round(offsetY);
        expectedTopLeftX = Math.Clamp(expectedTopLeftX, 0, baseW - 1);
        expectedTopLeftY = Math.Clamp(expectedTopLeftY, 0, baseH - 1);

        var topLeftColor = result.GetPixel(expectedTopLeftX, expectedTopLeftY);
        Assert.Equal(0, topLeftColor.Red);
        Assert.Equal(0, topLeftColor.Green);
        Assert.Equal(255, topLeftColor.Blue);
    }

    /// <summary>
    /// Tests that RenderSvgOverlay correctly applies padding and background color options when rendering overlays.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IImagingService.RenderSvgOverlay method's handling of ImagingOptions with Padding and BackgroundColor settings.</para>
    /// <para><strong>Data involved:</strong> A 120x80 gray base bitmap (RGB 200,200,200) and a 20x10 blue SVG rectangle. ImagingOptions specifies Padding=10 and BackgroundColor=green (RGB 0,255,0). The padding creates a 10-pixel border around the overlay area, and the background color should fill areas outside the padded overlay region.</para>
    /// <para><strong>Why the data matters:</strong> Padding and background colors are useful for creating visual spacing and backgrounds around overlays (e.g., badges with borders, watermarks with backgrounds). The renderer must correctly apply padding (reducing the available area for the overlay) and fill the padding area with the background color. This test validates that these styling options work correctly.</para>
    /// <para><strong>Expected outcome:</strong> Pixel (0,0) at the corner (outside the padded area) should be green (the background color). Pixel (11,11) inside the padded area should not be green (should be either the base bitmap color or overlay color).</para>
    /// <para><strong>Reason for expectation:</strong> The renderer should apply 10 pixels of padding, reducing the available area from 120x80 to 100x60. The padding area (0-10 pixels from edges) should be filled with the background color (green). The overlay should be rendered within the padded area (starting at pixel 10,10). The corner pixel being green confirms padding and background are applied, and the inside pixel not being green confirms the overlay/base bitmap is rendered within the padded area.</para>
    /// </remarks>
    [Fact]
    public void RenderSvgOverlayPaddingAndBackgroundAppliedCorrectly()
    {
        var services = new ServiceCollection();
        services.AddImagingServices();
        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IImagingService>();

        const int baseW = 120;
        const int baseH = 80;

        var baseInfo = new SKImageInfo(baseW, baseH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var baseBitmap = new SKBitmap(baseInfo);
        using (var c = new SKCanvas(baseBitmap)) c.Clear(new SKColor(200, 200, 200));

        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"10\">" +
                  "<rect x=\"0\" y=\"0\" width=\"20\" height=\"10\" fill=\"#0000FF\" />" +
                  "</svg>";

        var options = new ImagingOptions { FitMode = FitMode.BestFit, Anchor = Anchor.TopLeft, Padding = 10, BackgroundColor = new SKColor(0, 255, 0) };

        using var result = svc.RenderSvgOverlay(baseBitmap, svg, x: 0, y: 0, options: options);

        // Corners outside padded area should equal background color (0,255,0)
        var c1 = result.GetPixel(0, 0);
        Assert.Equal(0, c1.Red); Assert.Equal(255, c1.Green); Assert.Equal(0, c1.Blue);

        // Pixel inside padded area but where base is drawn should not be background; check somewhere near padded top-left + small offset
        var insideX = options.Padding + 1;
        var insideY = options.Padding + 1;
        var c2 = result.GetPixel(insideX, insideY);

        // c2 should be either base bitmap or overlay; ensure it's not the background
        Assert.False(c2.Equals(new SKColor(0, 255, 0)));
    }
}
