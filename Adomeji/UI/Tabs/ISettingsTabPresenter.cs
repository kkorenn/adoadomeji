using System;
using Adomeji.UI.Framework;
using UnityEngine;

namespace Adomeji.UI.Tabs;

internal interface ISettingsTabPresenter : IDisposable
{
  string Title { get; }
  void Build(RectTransform page, UiBuildContext context);
  void OnShown();
  void Refresh();
}
