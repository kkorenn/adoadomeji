using UnityEngine;

namespace Adomeji.UI.Input;

internal interface IKeyInput
{
  bool AnyKeyDown { get; }
  int FrameCount { get; }
  bool IsTextFieldFocused { get; }
  bool GetKeyDown(KeyCode key);
}
