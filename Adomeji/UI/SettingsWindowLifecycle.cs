using System;

namespace Adomeji.UI;

internal enum WindowShowAction
{
  None,
  Build,
  Activate,
}

internal sealed class SettingsWindowLifecycle
{
  public bool IsVisible { get; private set; }
  public bool IsBuilt { get; private set; }
  public bool IsDisposed { get; private set; }
  private bool _rebuildRequested;

  public WindowShowAction Show()
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(SettingsWindowLifecycle));
    if (IsVisible) return WindowShowAction.None;
    IsVisible = true;
    if (IsBuilt) return WindowShowAction.Activate;
    IsBuilt = true;
    return WindowShowAction.Build;
  }

  public bool Hide()
  {
    if (IsDisposed || !IsVisible) return false;
    IsVisible = false;
    return true;
  }

  public void RequestRebuild()
  {
    if (!IsDisposed) _rebuildRequested = true;
  }

  public bool ConsumeVisibleRebuild()
  {
    if (!_rebuildRequested || !IsVisible || IsDisposed) return false;
    _rebuildRequested = false;
    return true;
  }

  public void MarkViewDestroyed() => IsBuilt = false;
  public void MarkViewBuilt() => IsBuilt = true;

  public bool Dispose()
  {
    if (IsDisposed) return false;
    IsDisposed = true;
    IsVisible = false;
    IsBuilt = false;
    _rebuildRequested = false;
    return true;
  }
}
