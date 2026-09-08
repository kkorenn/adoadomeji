using Adomeji.Pets.Runtime;

namespace Adomeji.Gameplay.Events;

internal sealed class GameplayEventSink : IGameplayEventSink
{
  private readonly IPetRuntime _pets;

  public GameplayEventSink(IPetRuntime pets)
  {
    _pets = pets;
  }

  public void OnLevelStarted()
  {
    _pets.HandleLevelStarted();
  }

  public void OnJudgement(JudgementKind judgement)
  {
    _pets.HandleJudgement(judgement);
  }

  public void OnLevelCleared(bool purePerfect)
  {
    _pets.HandleLevelCleared(purePerfect);
  }

  public void OnLevelFailed()
  {
    _pets.HandleLevelFailed();
  }
}
