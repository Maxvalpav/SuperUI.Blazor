// SuperUI/Base/Services/IZIndexService.cs

namespace SuperUI.Base.Services;

public interface IZIndexService
{
    int GetNext();
    void Release(int zIndex);
    int Current { get; }
}
