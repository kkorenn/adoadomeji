using System;
using Adomeji.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Adomeji.UI.Input;

internal sealed class SettingsHotkeyController
{
  private readonly AdomejiSettings _settings;
  private readonly IKeyInput _input;
  private KeyCode[] _keys;
  private KeyCode? _toggleKey;
  private Text _captureLabel;
  private int _captureEndFrame = -1;

  public bool IsCapturing => _captureLabel != null;

  public SettingsHotkeyController(AdomejiSettings settings, IKeyInput input)
  {
    _settings = settings;
    _input = input;
  }

  public void BeginCapture(Text label)
  {
    _captureLabel = label;
    if (_captureLabel != null) _captureLabel.text = "Press a key... (Esc cancels)";
  }

  public void Clear(Text label)
  {
    _captureLabel = null;
    _settings.ToggleKey = "None";
    _toggleKey = KeyCode.None;
    if (label != null) label.text = "None";
  }

  public void CancelCapture()
  {
    if (_captureLabel != null) _captureLabel.text = _settings.ToggleKey;
    _captureLabel = null;
  }

  public void Tick(bool visible, Action toggle, Action hide)
  {
    if (IsCapturing)
    {
      CapturePressedKey();
      return;
    }

    if (visible && _input.GetKeyDown(KeyCode.Escape))
    {
      hide();
      return;
    }

    if (_input.FrameCount == _captureEndFrame || (visible && _input.IsTextFieldFocused))
      return;

    KeyCode key = ResolveToggleKey();
    if (key != KeyCode.None && _input.GetKeyDown(key)) toggle();
  }

  private KeyCode ResolveToggleKey()
  {
    if (_toggleKey != null) return _toggleKey.Value;
    try { _toggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), _settings.ToggleKey); }
    catch { _toggleKey = KeyCode.None; }
    return _toggleKey.Value;
  }

  private void CapturePressedKey()
  {
    if (!_input.AnyKeyDown) return;
    if (_input.GetKeyDown(KeyCode.Escape))
    {
      CancelCapture();
      return;
    }

    if (_keys == null) _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    for (int i = 0; i < _keys.Length; i++)
    {
      KeyCode key = _keys[i];
      if (key == KeyCode.None || key == KeyCode.Escape) continue;
      if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) continue;
      if (!_input.GetKeyDown(key)) continue;

      _settings.ToggleKey = key.ToString();
      _toggleKey = key;
      _captureEndFrame = _input.FrameCount;
      _captureLabel.text = _settings.ToggleKey;
      _captureLabel = null;
      return;
    }
  }
}
