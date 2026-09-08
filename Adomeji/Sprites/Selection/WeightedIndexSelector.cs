using System;

namespace Adomeji.Sprites.Selection;

internal static class WeightedIndexSelector
{
  public static int Select(int count, Func<int, float> weightAt, float unitRoll, int zeroWeightIndex)
  {
    if (count <= 0)
      return -1;

    float total = 0f;
    for (int i = 0; i < count; i++)
    {
      float weight = weightAt(i);
      if (weight > 0f)
        total += weight;
    }

    if (!(total > 0f))
      return ClampIndex(zeroWeightIndex, count);

    float roll = ClampUnit(unitRoll) * total;
    int lastPositive = 0;
    for (int i = 0; i < count; i++)
    {
      float weight = weightAt(i);
      if (weight <= 0f)
        continue;

      lastPositive = i;
      roll -= weight;
      if (roll <= 0f)
        return i;
    }

    return lastPositive;
  }

  private static float ClampUnit(float value)
  {
    if (value <= 0f) return 0f;
    if (value >= 1f) return 0.99999994f;
    return value;
  }

  private static int ClampIndex(int value, int count)
  {
    if (value < 0) return 0;
    if (value >= count) return count - 1;
    return value;
  }
}
