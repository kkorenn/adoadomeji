namespace Adomeji.Gameplay.Patches;

internal sealed class PatchCapabilities
{
  public bool LevelStarted { get; internal set; }
  public bool LevelCleared { get; internal set; }
  public bool LevelFailed { get; internal set; }
  public bool Judgements { get; internal set; }
  public bool Overpress { get; internal set; }
  public bool MouseSuppression { get; internal set; }
}
