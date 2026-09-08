using System;
using System.Collections.Generic;
using Adomeji.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Adomeji.UI.Tabs;

internal sealed class SettingsTabHost : IDisposable
{
  private readonly ISettingsTabPresenter[] _presenters;
  private readonly List<Button> _buttons = new List<Button>();
  private readonly List<GameObject> _pages = new List<GameObject>();
  private readonly UiElementFactory _ui;
  private readonly UiTheme _theme;
  private readonly UiBuildContext _context;

  public int ActiveIndex { get; private set; }

  public SettingsTabHost(
    ISettingsTabPresenter[] presenters,
    UiElementFactory ui,
    UiTheme theme,
    UiBuildContext context)
  {
    _presenters = presenters;
    _ui = ui;
    _theme = theme;
    _context = context;
  }

  public void Build(RectTransform tabBar, RectTransform contentArea, int selectedIndex)
  {
    for (int i = 0; i < _presenters.Length; i++)
    {
      int index = i;
      ISettingsTabPresenter presenter = _presenters[i];
      Button button = _ui.MakeButton(tabBar, "Tab_" + presenter.Title, presenter.Title, 18,
        () => Select(index));
      LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
      layout.flexibleWidth = 1;
      layout.minHeight = 42;
      _buttons.Add(button);

      RectTransform pageRoot = _ui.BuildScrollPage(contentArea, presenter.Title);
      _pages.Add(pageRoot.parent.parent.gameObject);
      presenter.Build(pageRoot, _context);
    }

    Select(Mathf.Clamp(selectedIndex, 0, _presenters.Length - 1));
  }

  public void Select(int index)
  {
    if (index < 0 || index >= _presenters.Length) return;
    ActiveIndex = index;
    for (int i = 0; i < _pages.Count; i++)
    {
      bool active = i == index;
      _pages[i].SetActive(active);
      Image image = _buttons[i].GetComponent<Image>();
      image.color = active ? _theme.Accent : _theme.PanelBackground;
      _buttons[i].GetComponentInChildren<Text>().color = active ? Color.black : _theme.DimText;
    }
    _presenters[index].OnShown();
  }

  public void RefreshActive()
  {
    if (ActiveIndex >= 0 && ActiveIndex < _presenters.Length)
      _presenters[ActiveIndex].Refresh();
  }

  public void Dispose()
  {
    for (int i = 0; i < _presenters.Length; i++) _presenters[i].Dispose();
    _buttons.Clear();
    _pages.Clear();
  }
}
