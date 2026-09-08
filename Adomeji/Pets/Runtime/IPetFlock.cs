using System;
using Adomeji.Gameplay.Events;

namespace Adomeji.Pets.Runtime;

internal interface IPetFlock : IDisposable
{
  void Tick(float deltaTime);
  void NotifyJudgement(JudgementKind judgement);
  void NotifyClear(bool purePerfect);
  void NotifyDeath();
}

internal interface IPetFlockFactory
{
  IPetFlock Create();
}
