using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Adomeji.UI.Framework;

internal sealed class UiResourceScope : IDisposable
{
  private readonly List<Sprite> _sprites = new List<Sprite>();

  public void Own(Sprite sprite)
  {
    if (sprite != null) _sprites.Add(sprite);
  }

  public void Clear()
  {
    for (int i = 0; i < _sprites.Count; i++)
    {
      Sprite sprite = _sprites[i];
      if (sprite == null) continue;
      if (sprite.texture != null) Object.Destroy(sprite.texture);
      Object.Destroy(sprite);
    }
    _sprites.Clear();
  }

  public void Dispose() => Clear();
}
