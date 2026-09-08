using System;

namespace Adomeji.UI;

internal interface ISettingsWindow : IDisposable
{
  bool Visible { get; }
  void Show();
  void Hide();
  void Toggle();
  void Tick();
}
