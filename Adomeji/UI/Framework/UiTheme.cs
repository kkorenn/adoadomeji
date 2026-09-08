using UnityEngine;

namespace Adomeji.UI.Framework;

internal sealed class UiTheme
{
  public Color WindowBackground { get; } = new Color(0.078f, 0.086f, 0.110f, 0.97f);
  public Color HeaderBackground { get; } = new Color(0.110f, 0.122f, 0.157f, 1f);
  public Color PanelBackground { get; } = new Color(0.137f, 0.149f, 0.188f, 1f);
  public Color Text { get; } = new Color(0.92f, 0.93f, 0.95f, 1f);
  public Color DimText { get; } = new Color(0.60f, 0.63f, 0.70f, 1f);
  public Color RowHover { get; } = new Color(1f, 1f, 1f, 0.04f);
  public Color Accent { get; }
  public Color AccentDim { get; }

  public UiTheme(string accentHex)
  {
    if (!ColorUtility.TryParseHtmlString("#" + accentHex, out Color accent))
      accent = new Color(1f, 0.420f, 0.320f, 1f);
    Accent = accent;
    accent.a = 0.35f;
    AccentDim = accent;
  }

  public Color PackColor(int index, int count) =>
    Color.HSVToRGB(count > 0 ? index / (float)count : 0f, 0.55f, 1f);
}
