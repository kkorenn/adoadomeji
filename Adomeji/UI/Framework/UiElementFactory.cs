using System;
using System.Globalization;
using Adomeji.UI.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Adomeji.UI.Framework;

internal sealed class UiElementFactory
{
  private readonly Font _font;
  private readonly UiTheme _theme;
  private readonly DynamicBindingRegistry _bindings;

  public UiElementFactory(Font font, UiTheme theme, DynamicBindingRegistry bindings)
  {
    _font = font;
    _theme = theme;
    _bindings = bindings;
  }

  public void AddSection(RectTransform page, string label)
  {
    RectTransform text = MakeText(page, "Section", label, 13,
      new Color(0.45f, 0.48f, 0.55f, 1f), TextAnchor.LowerLeft);
    text.gameObject.AddComponent<LayoutElement>().minHeight = 30;
  }

  public RectTransform BuildScrollPage(RectTransform parent, string name)
  {
      var root = NewRect("Page_" + name, parent);
      Stretch(root, 0, 0, 1, 1);

      var scroll = root.gameObject.AddComponent<ScrollRect>();
      scroll.horizontal = false;
      scroll.movementType = ScrollRect.MovementType.Clamped;
      scroll.scrollSensitivity = 30;

      var viewport = NewRect("Viewport", root);
      Stretch(viewport, 0, 0, 1, 1);
      viewport.gameObject.AddComponent<RectMask2D>();
      var vpImg = viewport.gameObject.AddComponent<Image>();
      vpImg.color = new Color(0, 0, 0, 0.001f); // raycast target for scroll wheel

      var content = NewRect("Rows", viewport);
      content.anchorMin = new Vector2(0, 1);
      content.anchorMax = new Vector2(1, 1);
      content.pivot = new Vector2(0.5f, 1);
      content.sizeDelta = Vector2.zero;       // kill the 100x100 default
      content.anchoredPosition = Vector2.zero;
      var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
      layout.padding = new RectOffset(22, 22, 16, 16);
      layout.spacing = 10;
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;
      content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
          ContentSizeFitter.FitMode.PreferredSize;

      scroll.viewport = viewport;
      scroll.content = content;
      return content;
  }

  /// <summary>Horizontal row container inside a page.</summary>
  public RectTransform AddRow(RectTransform page, float minHeight, int indent = 0)
  {
      var row = NewRect("Row", page);
      var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
      layout.padding = new RectOffset(indent, 0, 0, 0);
      layout.spacing = 10;
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = false;
      layout.childForceExpandHeight = false;
      layout.childAlignment = TextAnchor.MiddleLeft;
      row.gameObject.AddComponent<LayoutElement>().minHeight = minHeight;
      return row;
  }

  /// <summary>Full-width toggle row. Returns the row GameObject (for show/hide).</summary>
  public GameObject AddToggle(RectTransform page, string label,
      Func<bool> get, Action<bool> set, int indent = 0)
  {
      var row = NewRect("Toggle", page);
      row.gameObject.AddComponent<LayoutElement>().minHeight = 34;

      // invisible full-row image so the whole row is clickable
      var rowImg = row.gameObject.AddComponent<Image>();
      rowImg.color = new Color(1, 1, 1, 0f);

      var toggle = row.gameObject.AddComponent<Toggle>();

      var box = NewRect("Box", row);
      box.anchorMin = box.anchorMax = new Vector2(0, 0.5f);
      box.pivot = new Vector2(0, 0.5f);
      box.sizeDelta = new Vector2(24, 24);
      box.anchoredPosition = new Vector2(indent, 0);
      var boxImg = box.gameObject.AddComponent<Image>();
      boxImg.color = new Color(0.24f, 0.26f, 0.33f, 1f);
      boxImg.raycastTarget = false;

      var check = MakeText(box, "Check", "✓", 18, _theme.Accent, TextAnchor.MiddleCenter);
      Stretch(check, 0, 0, 1, 1);
      var checkImg = check.GetComponent<Text>();
      checkImg.raycastTarget = false;

      var text = MakeText(row, "Label", label, 17, _theme.Text, TextAnchor.MiddleLeft);
      Stretch(text, 0, 0, 1, 1);
      text.offsetMin = new Vector2(indent + 34, 0);
      text.offsetMax = new Vector2(0, 0);
      text.GetComponent<Text>().raycastTarget = false;

      toggle.targetGraphic = rowImg;
      toggle.graphic = checkImg;
      var colors = toggle.colors;
      colors.normalColor = new Color(1, 1, 1, 0f);
      colors.highlightedColor = _theme.RowHover;
      colors.pressedColor = new Color(1, 1, 1, 0.08f);
      colors.selectedColor = new Color(1, 1, 1, 0f);
      toggle.colors = colors;

      toggle.SetIsOnWithoutNotify(get());
      toggle.onValueChanged.AddListener(v =>
      {
          set(v);
          _bindings.Refresh();
      });
      _bindings.Add(() => toggle.SetIsOnWithoutNotify(get()));

      return row.gameObject;
  }

  /// <summary>Slider row with a type-in value field. Returns the row (for show/hide).</summary>
  public GameObject AddSlider(RectTransform page, string label,
      float min, Func<float> maxGetter, bool whole, string suffix,
      Func<float> get, Action<float> set, int indent = 0)
  {
      var row = AddRow(page, 36, indent);

      var text = MakeText(row, "Label", label, 17, _theme.Text, TextAnchor.MiddleLeft);
      text.gameObject.AddComponent<LayoutElement>().preferredWidth = 230 - indent;

      // ---- slider ----
      var sliderGo = NewRect("Slider", row);
      var sliderLayout = sliderGo.gameObject.AddComponent<LayoutElement>();
      sliderLayout.flexibleWidth = 1;
      sliderLayout.minHeight = 28;
      var slider = sliderGo.gameObject.AddComponent<Slider>();

      var bg = NewRect("Background", sliderGo);
      bg.anchorMin = new Vector2(0, 0.5f);
      bg.anchorMax = new Vector2(1, 0.5f);
      bg.sizeDelta = new Vector2(-8, 8);
      var bgImg = bg.gameObject.AddComponent<Image>();
      bgImg.color = _theme.PanelBackground;

      var fillArea = NewRect("Fill Area", sliderGo);
      fillArea.anchorMin = new Vector2(0, 0.5f);
      fillArea.anchorMax = new Vector2(1, 0.5f);
      fillArea.sizeDelta = new Vector2(-18, 8);
      var fill = NewRect("Fill", fillArea);
      fill.sizeDelta = new Vector2(6, 0);
      fill.gameObject.AddComponent<Image>().color = _theme.Accent;

      var handleArea = NewRect("Handle Area", sliderGo);
      handleArea.anchorMin = new Vector2(0, 0.5f);
      handleArea.anchorMax = new Vector2(1, 0.5f);
      handleArea.sizeDelta = new Vector2(-18, 0);
      var handle = NewRect("Handle", handleArea);
      handle.sizeDelta = new Vector2(20, 20);
      var handleImg = handle.gameObject.AddComponent<Image>();
      handleImg.color = _theme.Text;

      slider.fillRect = fill;
      slider.handleRect = handle;
      slider.targetGraphic = handleImg;
      slider.direction = Slider.Direction.LeftToRight;
      slider.minValue = min;
      slider.maxValue = maxGetter();
      slider.wholeNumbers = whole;
      slider.SetValueWithoutNotify(get());

      // ---- value field ----
      var fieldGo = NewRect("Value", row);
      var fieldLe = fieldGo.gameObject.AddComponent<LayoutElement>();
      fieldLe.preferredWidth = 68;
      // a sprite-less Image reports preferredHeight 0, and the row layout
      // (childControlHeight on, forceExpand off) takes that literally —
      // without an explicit height the whole field collapses to 0px and
      // the value text is invisible
      fieldLe.preferredHeight = 28;
      var fieldImg = fieldGo.gameObject.AddComponent<Image>();
      fieldImg.color = _theme.PanelBackground;
      var input = fieldGo.gameObject.AddComponent<InputField>();

      var fieldText = MakeText(fieldGo, "Text", "", 16, _theme.Text, TextAnchor.MiddleCenter);
      Stretch(fieldText, 0, 0, 1, 1);
      fieldText.offsetMin = new Vector2(6, 2);
      fieldText.offsetMax = new Vector2(-6, -2);
      var fieldTextCmp = fieldText.GetComponent<Text>();
      // SingleLine is already the default, so InputField's lineType setter
      // short-circuits and never calls EnforceTextHOverflow() for us. Left
      // on Wrap the label measures against a not-yet-laid-out (0-wide) rect
      // and draws nothing.
      fieldTextCmp.horizontalOverflow = HorizontalWrapMode.Overflow;
      fieldTextCmp.supportRichText = false;
      input.textComponent = fieldTextCmp;
      input.contentType = whole ? InputField.ContentType.IntegerNumber
                                : InputField.ContentType.DecimalNumber;
      input.lineType = InputField.LineType.SingleLine;

      if (suffix != null)
      {
          var suffixText = MakeText(row, "Suffix", suffix, 16, _theme.DimText, TextAnchor.MiddleLeft);
          suffixText.gameObject.AddComponent<LayoutElement>().preferredWidth = 18;
      }

      string Format(float v) => whole
          ? Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture)
          : v.ToString("0.0", CultureInfo.InvariantCulture);

      // InputField.UpdateLabel() clips the label to the rect it measures at
      // call time; during Build() layout hasn't run, the rect is 0 wide, and
      // the clipped result ("") sticks — later layout passes redraw the Text
      // but never re-run UpdateLabel. So while the field is NOT focused we
      // bypass the InputField label pipeline and write the Text directly;
      // once the user focuses it, the rect is valid and InputField takes over.
      void SyncLabel(string s)
      {
          input.SetTextWithoutNotify(s);
          if (!input.isFocused) fieldTextCmp.text = s;
      }

      SyncLabel(Format(get()));

      slider.onValueChanged.AddListener(v =>
      {
          if (whole) v = Mathf.Round(v);
          set(v);
          SyncLabel(Format(v));
          _bindings.Refresh();
      });

      input.onEndEdit.AddListener(typed =>
      {
          if (float.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture,
                  out float parsed))
          {
              parsed = Mathf.Clamp(whole ? Mathf.Round(parsed) : parsed, min, maxGetter());
              set(parsed);
              slider.SetValueWithoutNotify(parsed);
              _bindings.Refresh();
          }
          SyncLabel(Format(get()));
      });

      // keep cap + shown value in sync when other settings change the max
      _bindings.Add(() =>
      {
          slider.maxValue = maxGetter();
          slider.SetValueWithoutNotify(get());
          if (!input.isFocused) SyncLabel(Format(get()));
      });

      return row.gameObject;
  }

  public void AddHelp(RectTransform page, string body)
  {
      var text = MakeText(page, "Help", body, 15, _theme.DimText, TextAnchor.UpperLeft);
      var t = text.GetComponent<Text>();
      t.horizontalOverflow = HorizontalWrapMode.Wrap;
      t.verticalOverflow = VerticalWrapMode.Overflow;
  }

  // ---- primitives ----------------------------------------------------

  public RectTransform NewRect(string name, Transform parent)
  {
      var go = new GameObject(name, typeof(RectTransform));
      go.transform.SetParent(parent, false);
      return (RectTransform)go.transform;
  }

  public void Stretch(RectTransform rect,
      float minX, float minY, float maxX, float maxY)
  {
      rect.anchorMin = new Vector2(minX, minY);
      rect.anchorMax = new Vector2(maxX, maxY);
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;
  }

  public RectTransform MakeText(Transform parent, string name, string content,
      int size, Color color, TextAnchor anchor)
  {
      var rect = NewRect(name, parent);
      var text = rect.gameObject.AddComponent<Text>();
      text.font = _font;
      text.fontSize = size;
      text.color = color;
      text.alignment = anchor;
      text.supportRichText = true;
      text.text = content;
      return rect;
  }

  public Button MakeButton(Transform parent, string name, string label,
      int fontSize, UnityAction onClick)
  {
      var rect = NewRect(name, parent);
      var img = rect.gameObject.AddComponent<Image>();
      img.color = _theme.PanelBackground;
      var button = rect.gameObject.AddComponent<Button>();
      button.targetGraphic = img;
      var colors = button.colors;
      colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
      colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
      button.colors = colors;
      button.onClick.AddListener(onClick);

      var text = MakeText(rect, "Text", label, fontSize, _theme.Text, TextAnchor.MiddleCenter);
      Stretch(text, 0, 0, 1, 1);
      text.offsetMin = new Vector2(10, 0);
      text.offsetMax = new Vector2(-10, 0);
      text.GetComponent<Text>().raycastTarget = false;
      return button;
  }

  public static Font LoadFont()
  {
      // built-in font name changed across Unity versions; try both, then OS
      try
      {
          var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
          if (f != null) return f;
      }
      catch { }
      try
      {
          var f = Resources.GetBuiltinResource<Font>("Arial.ttf");
          if (f != null) return f;
      }
      catch { }
      return Font.CreateDynamicFontFromOSFont("Arial", 17);
  }

  // ---- behaviours ----------------------------------------------------

  /// <summary>Per-frame duties while the window is open.</summary>
}
