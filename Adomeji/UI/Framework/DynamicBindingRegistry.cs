using System;
using System.Collections.Generic;

namespace Adomeji.UI.Framework;

internal sealed class DynamicBindingRegistry : IDisposable
{
  private readonly List<Action> _bindings = new List<Action>();

  public void Add(Action binding)
  {
    if (binding != null) _bindings.Add(binding);
  }

  public void Refresh()
  {
    for (int i = 0; i < _bindings.Count; i++) _bindings[i]();
  }

  public void Dispose() => _bindings.Clear();
}
