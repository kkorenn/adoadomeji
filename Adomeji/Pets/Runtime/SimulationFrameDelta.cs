namespace Adomeji.Pets.Runtime;

internal static class SimulationFrameDelta
{
  public static float Resolve(float callbackDeltaTime, float unscaledDeltaTime)
  {
    if (!float.IsNaN(unscaledDeltaTime) && !float.IsInfinity(unscaledDeltaTime)
        && unscaledDeltaTime > 0f)
      return unscaledDeltaTime;

    if (!float.IsNaN(callbackDeltaTime) && !float.IsInfinity(callbackDeltaTime)
        && callbackDeltaTime > 0f)
      return callbackDeltaTime;

    return 0f;
  }
}
