using System;
using Adomeji.Gameplay.Events;

namespace Adomeji.Pets.Runtime;

internal interface IPetRuntime : IDisposable
{
  void Start();

  void Tick(float deltaTime);

  void Stop();

  void HandleLevelStarted();

  void HandleJudgement(JudgementKind judgement);

  void HandleLevelCleared(bool purePerfect);

  void HandleLevelFailed();
}
