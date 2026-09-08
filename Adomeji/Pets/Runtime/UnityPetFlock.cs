using Adomeji.Gameplay.Events;
using Adomeji.Pets.Surfaces;

namespace Adomeji.Pets.Runtime;

internal sealed class UnityPetFlockFactory : IPetFlockFactory
{
  public IPetFlock Create() => new UnityPetFlock(PetFlockController.Create());
}

internal sealed class UnityPetFlock : IPetFlock
{
  private PetFlockController _controller;

  public UnityPetFlock(PetFlockController controller)
  {
    _controller = controller;
  }

  public void Tick(float deltaTime) => _controller?.Tick(deltaTime);
  public void NotifyJudgement(JudgementKind judgement) =>
    PetFlockController.NotifyJudgement(judgement.ToString());
  public void NotifyClear(bool purePerfect) => PetFlockController.NotifyClear(purePerfect);
  public void NotifyDeath() => PetFlockController.NotifyDeath();

  public void Dispose()
  {
    if (_controller == null) return;
    PetFlockController.DestroyAll();
    _controller = null;
  }
}
