using System;

namespace Adomeji.Pets.Physics;

internal static class SweptCollision
{
  public static bool SegmentIntersectsCircle(
    float startX, float startY,
    float endX, float endY,
    float centerX, float centerY,
    float radius,
    out float closestX,
    out float closestY)
  {
    float deltaX = endX - startX;
    float deltaY = endY - startY;
    float lengthSquared = deltaX * deltaX + deltaY * deltaY;
    float t = lengthSquared > 0.001f
      ? ((centerX - startX) * deltaX + (centerY - startY) * deltaY) / lengthSquared
      : 1f;
    t = Math.Max(0f, Math.Min(1f, t));
    closestX = startX + deltaX * t;
    closestY = startY + deltaY * t;
    float distanceX = centerX - closestX;
    float distanceY = centerY - closestY;
    return distanceX * distanceX + distanceY * distanceY <= radius * radius;
  }
}
