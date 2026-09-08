namespace Adomeji.Gameplay.Events;

internal interface IGameplayEventSink
{
  void OnLevelStarted();
  void OnJudgement(JudgementKind judgement);
  void OnLevelCleared(bool purePerfect);
  void OnLevelFailed();
}
