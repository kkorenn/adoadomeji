using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Adomeji.UI.Input;

internal sealed class UnityKeyInput : IKeyInput
{
  public bool AnyKeyDown => UnityEngine.Input.anyKeyDown;
  public int FrameCount => Time.frameCount;
  public bool GetKeyDown(KeyCode key) => UnityEngine.Input.GetKeyDown(key);

  public bool IsTextFieldFocused
  {
    get
    {
      if (EventSystem.current == null) return false;
      GameObject selected = EventSystem.current.currentSelectedGameObject;
      InputField inputField = selected != null ? selected.GetComponent<InputField>() : null;
      return inputField != null && inputField.isFocused;
    }
  }
}
