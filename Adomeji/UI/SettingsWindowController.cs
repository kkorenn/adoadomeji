using System;
using Adomeji.Composition;
using Adomeji.Settings;
using Adomeji.Sprites.Catalog;
using Adomeji.Sprites.Loading;
using Adomeji.UI.Components;
using Adomeji.UI.Framework;
using Adomeji.UI.Input;
using Adomeji.UI.Services;
using Adomeji.UI.Tabs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Adomeji.UI;

internal sealed class SettingsWindowController : ISettingsWindow
{
  private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

  private readonly ISettingsStore _settingsStore;
  private readonly ISpritePackCatalog _catalog;
  private readonly ISpritePackLoader _loader;
  private readonly IPetCustomizationService _pets;
  private readonly ISpriteRoleStore _roles;
  private readonly IUpdatePreferencesStore _updatePreferences;
  private readonly SettingsHotkeyController _hotkeys;
  private readonly SettingsWindowLifecycle _lifecycle = new SettingsWindowLifecycle();

  private GameObject _canvasObject;
  private GameObject _windowObject;
  private GameObject _ownedEventSystem;
  private DynamicBindingRegistry _bindings;
  private SettingsTabHost _tabs;
  private Vector2? _savedSize;
  private Vector2? _savedPosition;
  private int _savedTab;
  public bool Visible => _lifecycle.IsVisible && _windowObject != null && _windowObject.activeSelf;

  public SettingsWindowController(
    ISettingsStore settingsStore,
    ISpritePackCatalog catalog,
    ISpritePackLoader loader,
    IPetCustomizationService pets,
    ISpriteRoleStore roles,
    IUpdatePreferencesStore updatePreferences,
    IKeyInput keyInput)
  {
    _settingsStore = settingsStore;
    _catalog = catalog;
    _loader = loader;
    _pets = pets;
    _roles = roles;
    _updatePreferences = updatePreferences;
    _hotkeys = new SettingsHotkeyController(settingsStore.Current, keyInput);
  }

  public void Show()
  {
    WindowShowAction action = _lifecycle.Show();
    if (action == WindowShowAction.None) return;
    if (action == WindowShowAction.Build) BuildView();
    _windowObject.SetActive(true);
    Canvas.ForceUpdateCanvases();
    _bindings.Refresh();
    _tabs.RefreshActive();
  }

  public void Hide()
  {
    if (!_lifecycle.Hide()) return;
    _windowObject.SetActive(false);
    _hotkeys.CancelCapture();
    _settingsStore.Save();
    _updatePreferences.Save();
  }

  public void Toggle()
  {
    if (Visible) Hide();
    else Show();
  }

  public void Tick()
  {
    if (_lifecycle.IsDisposed) return;
    _hotkeys.Tick(Visible, Toggle, Hide);
    if (_lifecycle.ConsumeVisibleRebuild()) RebuildView();
  }

  public void Dispose()
  {
    bool wasVisible = Visible;
    if (!_lifecycle.Dispose()) return;
    if (wasVisible) _settingsStore.Save();
    if (wasVisible) _updatePreferences.Save();
    DestroyView();
    if (_ownedEventSystem != null) Object.Destroy(_ownedEventSystem);
    _ownedEventSystem = null;
  }

  private void BuildView()
  {
    AdomejiSettings settings = _settingsStore.Current;
    UiTheme theme = new UiTheme(settings.AccentHex);
    _bindings = new DynamicBindingRegistry();
    Font font = UiElementFactory.LoadFont();
    var ui = new UiElementFactory(font, theme, _bindings);
    var context = new UiBuildContext(settings, ui, _bindings, theme);

    _canvasObject = new GameObject("AdomejiSettingsCanvas");
    Object.DontDestroyOnLoad(_canvasObject);
    Canvas canvas = _canvasObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 31000;
    CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = ReferenceResolution;
    scaler.matchWidthOrHeight = 0.5f;
    _canvasObject.AddComponent<GraphicRaycaster>();
    EnsureEventSystem();

    _windowObject = new GameObject("Window", typeof(RectTransform));
    _windowObject.transform.SetParent(_canvasObject.transform, false);
    RectTransform window = (RectTransform)_windowObject.transform;
    window.sizeDelta = _savedSize ?? new Vector2(860f, 640f);
    window.anchoredPosition = _savedPosition ?? Vector2.zero;
    window.localScale = Vector3.one * Mathf.Clamp(settings.UiScale, 0.6f, 1.6f);
    _windowObject.AddComponent<Image>().color = theme.WindowBackground;
    _windowObject.AddComponent<WindowDragHandle>().Target = window;

    RectTransform header = ui.NewRect("Header", window);
    ui.Stretch(header, 0f, 1f, 1f, 1f);
    header.sizeDelta = new Vector2(0f, 52f);
    header.pivot = new Vector2(0.5f, 1f);
    header.anchoredPosition = Vector2.zero;
    header.gameObject.AddComponent<Image>().color = theme.HeaderBackground;
    header.gameObject.AddComponent<WindowDragHandle>().Target = window;

    RectTransform title = ui.MakeText(header, "Title",
      "Adomeji  <color=#9aa0ac>v" + RuntimeContext.Version + "</color>",
      22, theme.Text, TextAnchor.MiddleLeft);
    ui.Stretch(title, 0f, 0f, 1f, 1f);
    title.offsetMin = new Vector2(18f, 0f);
    title.offsetMax = new Vector2(-60f, 0f);
    title.GetComponent<Text>().raycastTarget = false;

    Button close = ui.MakeButton(header, "Close", "✕", 20, Hide);
    RectTransform closeRect = (RectTransform)close.transform;
    closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
    closeRect.pivot = new Vector2(1f, 0.5f);
    closeRect.sizeDelta = new Vector2(40f, 36f);
    closeRect.anchoredPosition = new Vector2(-8f, 0f);
    close.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

    RectTransform tabBar = ui.NewRect("TabBar", window);
    ui.Stretch(tabBar, 0f, 1f, 1f, 1f);
    tabBar.sizeDelta = new Vector2(0f, 44f);
    tabBar.pivot = new Vector2(0.5f, 1f);
    tabBar.anchoredPosition = new Vector2(0f, -52f);
    HorizontalLayoutGroup tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
    tabLayout.padding = new RectOffset(12, 12, 6, 0);
    tabLayout.spacing = 6f;
    tabLayout.childControlWidth = true;
    tabLayout.childControlHeight = true;
    tabLayout.childForceExpandWidth = true;
    tabLayout.childForceExpandHeight = true;

    RectTransform content = ui.NewRect("Content", window);
    ui.Stretch(content, 0f, 0f, 1f, 1f);
    content.offsetMin = Vector2.zero;
    content.offsetMax = new Vector2(0f, -96f);

    ISettingsTabPresenter[] presenters =
    {
      new SpritesTabPresenter(_catalog, _loader, _pets, _roles),
      new BehaviorTabPresenter(),
      new AppearanceTabPresenter(_hotkeys, _updatePreferences, RequestRebuild, SetWindowScale),
      new CreditsTabPresenter(),
    };
    _tabs = new SettingsTabHost(presenters, ui, theme, context);
    _tabs.Build(tabBar, content, _savedTab);
    BuildResizeHandles(window, ui);
    _lifecycle.MarkViewBuilt();
  }

  private void RebuildView()
  {
    CaptureWindowState();
    DestroyView();
    BuildView();
    _windowObject.SetActive(true);
    Canvas.ForceUpdateCanvases();
    _bindings.Refresh();
  }

  private void DestroyView()
  {
    _tabs?.Dispose();
    _tabs = null;
    _bindings?.Dispose();
    _bindings = null;
    if (_canvasObject != null) Object.Destroy(_canvasObject);
    _canvasObject = null;
    _windowObject = null;
    _lifecycle.MarkViewDestroyed();
  }

  private void CaptureWindowState()
  {
    if (_windowObject == null) return;
    RectTransform window = (RectTransform)_windowObject.transform;
    _savedSize = window.sizeDelta;
    _savedPosition = window.anchoredPosition;
    if (_tabs != null) _savedTab = _tabs.ActiveIndex;
  }

  private void RequestRebuild() => _lifecycle.RequestRebuild();

  private void SetWindowScale(float scale)
  {
    if (_windowObject != null) _windowObject.transform.localScale = Vector3.one * scale;
  }

  private void EnsureEventSystem()
  {
    if (Object.FindAnyObjectByType<EventSystem>() != null) return;
    _ownedEventSystem = new GameObject("AdomejiEventSystem",
      typeof(EventSystem), typeof(StandaloneInputModule));
    Object.DontDestroyOnLoad(_ownedEventSystem);
  }

  private static void BuildResizeHandles(RectTransform window, UiElementFactory ui)
  {
    const float edge = 10f;
    const float corner = 20f;
    for (int x = -1; x <= 1; x++)
    for (int y = -1; y <= 1; y++)
    {
      if (x == 0 && y == 0) continue;
      float anchorX = (x + 1) * 0.5f;
      float anchorY = (y + 1) * 0.5f;
      RectTransform handleRect = ui.NewRect("Resize_" + x + "_" + y, window);
      if (x != 0 && y != 0)
      {
        handleRect.anchorMin = handleRect.anchorMax = new Vector2(anchorX, anchorY);
        handleRect.pivot = new Vector2(anchorX, anchorY);
        handleRect.sizeDelta = new Vector2(corner, corner);
      }
      else if (x != 0)
      {
        handleRect.anchorMin = new Vector2(anchorX, 0f);
        handleRect.anchorMax = new Vector2(anchorX, 1f);
        handleRect.pivot = new Vector2(anchorX, 0.5f);
        handleRect.sizeDelta = new Vector2(edge, -2f * corner);
      }
      else
      {
        handleRect.anchorMin = new Vector2(0f, anchorY);
        handleRect.anchorMax = new Vector2(1f, anchorY);
        handleRect.pivot = new Vector2(0.5f, anchorY);
        handleRect.sizeDelta = new Vector2(-2f * corner, edge);
      }
      handleRect.anchoredPosition = Vector2.zero;
      handleRect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
      WindowResizeHandle handle = handleRect.gameObject.AddComponent<WindowResizeHandle>();
      handle.Target = window;
      handle.XDirection = x;
      handle.YDirection = y;
    }
  }

}
