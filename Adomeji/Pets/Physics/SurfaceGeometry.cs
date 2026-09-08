namespace Adomeji.Pets.Physics;

internal static class SurfaceGeometry
{
  public static float TopAt(float[] xPoints, float[] yPoints, int offset, int count, float x)
  {
    float best = float.NegativeInfinity;
    for (int edge = 0; edge < count; edge++)
    {
      int next = edge + 1 == count ? 0 : edge + 1;
      float x0 = xPoints[offset + edge];
      float x1 = xPoints[offset + next];
      if (x0 < x1) { if (x < x0 || x > x1) continue; }
      else if (x0 > x1) { if (x < x1 || x > x0) continue; }
      else continue;

      float y0 = yPoints[offset + edge];
      float y1 = yPoints[offset + next];
      float y = y0 + (y1 - y0) * ((x - x0) / (x1 - x0));
      if (y > best) best = y;
    }
    return best;
  }
}
