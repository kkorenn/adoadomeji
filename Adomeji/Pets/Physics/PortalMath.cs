namespace Adomeji.Pets.Physics;

internal static class PortalMath
{
  public static float Wrap(float position, float halfSize, float extent)
  {
    float span = extent + halfSize * 2f;
    if (position < -halfSize) return position + span;
    if (position > extent + halfSize) return position - span;
    return position;
  }
}
