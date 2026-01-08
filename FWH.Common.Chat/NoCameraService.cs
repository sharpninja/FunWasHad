using FWH.Common.Chat.Services;
using System.Threading.Tasks;

namespace FWH.Common.Chat.Services;

/// <summary>
/// Fallback camera service for desktop platforms (no camera available)
/// </summary>
public class NoCameraService : ICameraService
{
    public NoCameraService()
    {
    }

    public bool IsCameraAvailable => false;

    public Task<byte[]?> TakePhotoAsync()
    {
        return Task.FromResult<byte[]?>(null);
    }
}
