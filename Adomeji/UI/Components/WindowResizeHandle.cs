using UnityEngine;
using UnityEngine.EventSystems;

namespace Adomeji.UI.Components;

internal sealed class WindowResizeHandle : MonoBehaviour, IDragHandler
{
  private const float MinimumWidth = 560f;
  private const float MinimumHeight = 360f;
  private const float MaximumWidth = 1880f;
  private const float MaximumHeight = 1040f;

  public RectTransform Target { get; set; }
  public int XDirection { get; set; }
  public int YDirection { get; set; }

  public void OnDrag(PointerEventData eventData)
  {
    if (Target == null) return;
    Vector2 delta = eventData.delta / Target.lossyScale.x;
    Vector2 size = Target.sizeDelta;
    Vector2 position = Target.anchoredPosition;
    float scale = Target.localScale.x;

    if (XDirection != 0)
    {
      float width = Mathf.Clamp(size.x + delta.x * XDirection, MinimumWidth, MaximumWidth);
      float grown = width - size.x;
      size.x = width;
      position.x += XDirection * grown * 0.5f * scale;
    }
    if (YDirection != 0)
    {
      float height = Mathf.Clamp(size.y + delta.y * YDirection, MinimumHeight, MaximumHeight);
      float grown = height - size.y;
      size.y = height;
      position.y += YDirection * grown * 0.5f * scale;
    }

    Target.sizeDelta = size;
    Target.anchoredPosition = position;
  }
}
