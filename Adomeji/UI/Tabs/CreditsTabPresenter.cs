using Adomeji.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Adomeji.UI.Tabs;

internal sealed class CreditsTabPresenter : ISettingsTabPresenter
{
  private const string OriginalProjectUrl = "https://github.com/kkorenn/adomeji";
  public string Title => "Credits";

  public void Build(RectTransform page, UiBuildContext context)
  {
    context.Elements.AddSection(page, "CREDITS");
    AddLine(page, context, "Original Adomeji by koren");
    AddLine(page, context, "Modified version by IMPL_");

    Button original = context.Elements.MakeButton(
      page, "OriginalProject", "View original project", 17,
      () => Application.OpenURL(OriginalProjectUrl));
    LayoutElement layout = original.gameObject.AddComponent<LayoutElement>();
    layout.minHeight = 40;
    layout.preferredWidth = 260;

    context.Elements.AddHelp(page,
      "The upstream MIT license and attribution notices are included with every release.");
  }

  public void OnShown() { }
  public void Refresh() { }
  public void Dispose() { }

  private static void AddLine(RectTransform page, UiBuildContext context, string value)
  {
    RectTransform line = context.Elements.MakeText(
      page, "Credit", value, 18, context.Theme.Text, TextAnchor.MiddleLeft);
    line.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
  }
}
