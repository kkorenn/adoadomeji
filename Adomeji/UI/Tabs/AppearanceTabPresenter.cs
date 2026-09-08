using System;
using Adomeji.Settings;
using Adomeji.UI.Framework;
using Adomeji.UI.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Adomeji.UI.Tabs;

internal sealed class AppearanceTabPresenter : ISettingsTabPresenter
{
  private static readonly string[] AccentPresets =
  {
    "FF6B52", "FFA94D", "FFD43B", "51CF66",
    "22B8CF", "4D8DFF", "9775FA", "F783AC",
  };

  private readonly SettingsHotkeyController _hotkeys;
  private readonly IUpdatePreferencesStore _updatePreferences;
  private readonly Action _requestRebuild;
  private readonly Action<float> _setWindowScale;
  private AdomejiSettings _settings;
  private UiElementFactory _ui;
  private DynamicBindingRegistry _bindings;

  public string Title => "UI Settings";

  public AppearanceTabPresenter(
    SettingsHotkeyController hotkeys,
    IUpdatePreferencesStore updatePreferences,
    Action requestRebuild,
    Action<float> setWindowScale)
  {
    _hotkeys = hotkeys;
    _updatePreferences = updatePreferences;
    _requestRebuild = requestRebuild;
    _setWindowScale = setWindowScale;
  }

  public void Build(RectTransform page, UiBuildContext context)
  {
    _settings = context.Settings;
    _ui = context.Elements;
    _bindings = context.Bindings;

    _ui.AddSection(page, "INTERFACE");
    _ui.AddSlider(page, "UI scale", 0.6f, () => 1.6f, false, "x",
      () => _settings.UiScale,
      value =>
      {
        _settings.UiScale = value;
        _setWindowScale(value);
      });

    RectTransform swatchRow = _ui.AddRow(page, 42);
    RectTransform swatchLabel = _ui.MakeText(
      swatchRow, "Label", "Accent color", 17, context.Theme.Text, TextAnchor.MiddleLeft);
    swatchLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 230;

    for (int i = 0; i < AccentPresets.Length; i++)
    {
      string hex = AccentPresets[i];
      ColorUtility.TryParseHtmlString("#" + hex, out Color color);
      bool selected = string.Equals(hex, _settings.AccentHex, StringComparison.OrdinalIgnoreCase);
      Button swatch = _ui.MakeButton(swatchRow, "Swatch", selected ? "✓" : "", 16, () =>
      {
        _settings.AccentHex = hex;
        _requestRebuild();
      });
      LayoutElement layout = swatch.gameObject.AddComponent<LayoutElement>();
      layout.preferredWidth = 34;
      layout.preferredHeight = 34;
      swatch.GetComponent<Image>().color = color;
      swatch.GetComponentInChildren<Text>().color = Color.black;
    }

    RectTransform keyRow = _ui.AddRow(page, 40);
    RectTransform keyLabel = _ui.MakeText(
      keyRow, "Label", "Toggle window key", 17, context.Theme.Text, TextAnchor.MiddleLeft);
    keyLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 230;

    Text bindLabel = null;
    Button bind = _ui.MakeButton(keyRow, "Bind", _settings.ToggleKey, 17,
      () => _hotkeys.BeginCapture(bindLabel));
    LayoutElement bindLayout = bind.gameObject.AddComponent<LayoutElement>();
    bindLayout.preferredWidth = 260;
    bindLayout.minHeight = 34;
    bindLabel = bind.GetComponentInChildren<Text>();

    Button clear = _ui.MakeButton(keyRow, "Clear", "Clear", 15,
      () => _hotkeys.Clear(bindLabel));
    LayoutElement clearLayout = clear.gameObject.AddComponent<LayoutElement>();
    clearLayout.preferredWidth = 80;
    clearLayout.minHeight = 34;

    _ui.AddSection(page, "UPDATES");
    _ui.AddToggle(page, "Receive beta updates",
      () => _updatePreferences.ReceiveBetaUpdates,
      value =>
      {
        _updatePreferences.ReceiveBetaUpdates = value;
        _updatePreferences.Save();
      });
    RectTransform updateNote = _ui.MakeText(page, "UpdateNote",
      "Beta channel changes apply on the next game launch.", 14,
      context.Theme.DimText, TextAnchor.MiddleLeft);
    updateNote.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
  }

  public void OnShown() => Refresh();
  public void Refresh() => _bindings?.Refresh();
  public void Dispose() => _hotkeys.CancelCapture();
}
