using Adomeji.Gameplay.Events;
using Adomeji.Shared.Diagnostics;

namespace Adomeji.Pets.Runtime;

internal sealed class PetRuntime : IPetRuntime
{
  private readonly IModLogger _logger;
  private readonly IPetFlockFactory _factory;
  private IPetFlock _flock;
  private bool _started;

  public PetRuntime(IModLogger logger, IPetFlockFactory factory = null)
  {
    _logger = logger;
    _factory = factory ?? new UnityPetFlockFactory();
  }

  public void Start()
  {
    if (_started) return;

    _flock = _factory.Create();
    _started = true;
    _logger.Info("[Pets] Runtime started");
  }

  public void Tick(float deltaTime)
  {
    if (!_started || _flock == null) return;
    _flock.Tick(deltaTime);
  }

  public void Stop()
  {
    if (!_started) return;

    _flock?.Dispose();
    _flock = null;
    _started = false;
    _logger.Info("[Pets] Runtime stopped");
  }

  public void HandleLevelStarted()
  {
    if (!_started) return;
    _logger.Info("[Pets] Level started");
  }

  public void HandleJudgement(JudgementKind judgement)
  {
    if (!_started) return;
    _flock.NotifyJudgement(judgement);
  }

  public void HandleLevelCleared(bool purePerfect)
  {
    if (!_started) return;
    _flock.NotifyClear(purePerfect);
  }

  public void HandleLevelFailed()
  {
    if (!_started) return;
    _flock.NotifyDeath();
  }

  public void Dispose()
  {
    Stop();
  }
}
