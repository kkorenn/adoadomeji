using UnityEngine;
using UnityEngine.EventSystems;

namespace Adomeji.UI.Components;

internal sealed class WindowDragHandle : MonoBehaviour, IDragHandler
{
  public RectTransform Target { get; set; }

  public void OnDrag(PointerEventData eventData)
  {
    if (Target == null) return;
    float canvasScale = Target.parent.lossyScale.x;
    Target.anchoredPosition += eventData.delta / canvasScale;
  }
}
