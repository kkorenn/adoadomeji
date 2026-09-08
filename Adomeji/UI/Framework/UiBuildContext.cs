using Adomeji.Settings;

namespace Adomeji.UI.Framework;

internal sealed class UiBuildContext
{
  public AdomejiSettings Settings { get; }
  public UiElementFactory Elements { get; }
  public DynamicBindingRegistry Bindings { get; }
  public UiTheme Theme { get; }

  public UiBuildContext(
    AdomejiSettings settings,
    UiElementFactory elements,
    DynamicBindingRegistry bindings,
    UiTheme theme)
  {
    Settings = settings;
    Elements = elements;
    Bindings = bindings;
    Theme = theme;
  }
}
