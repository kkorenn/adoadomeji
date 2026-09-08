using Adomeji.Settings;
using Adomeji.UI.Framework;
using UnityEngine;

namespace Adomeji.UI.Tabs;

internal sealed class BehaviorTabPresenter : ISettingsTabPresenter
{
  private AdomejiSettings _settings;
  private UiElementFactory _ui;
  private DynamicBindingRegistry _bindings;

  public string Title => "Behavior";

  public void Build(RectTransform page, UiBuildContext context)
  {
      _settings = context.Settings;
      _ui = context.Elements;
      _bindings = context.Bindings;

      _ui.AddToggle(page, "Pick up pets with the mouse",
          () => _settings.AllowDrag,
          v => _settings.AllowDrag = v);
      _ui.AddToggle(page, "Dragging a pet blocks that click from the game",
          () => _settings.SwallowMouse,
          v => _settings.SwallowMouse = v);
      _ui.AddToggle(page, "Pets bump into each other on tiles, decorations and planets",
          () => _settings.PetCollisionsTiles,
          v => _settings.PetCollisionsTiles = v);
      _ui.AddToggle(page, "Pets bump into each other on the screen floor and walls",
          () => _settings.PetCollisionsScreen,
          v => _settings.PetCollisionsScreen = v);

      _ui.AddToggle(page, "Remove the floor",
          () => _settings.NoFloor,
          v => _settings.NoFloor = v);
      _ui.AddToggle(page, "Remove the walls",
          () => _settings.NoWall,
          v => _settings.NoWall = v);
      _ui.AddToggle(page, "Remove the ceiling",
          () => _settings.NoCeiling,
          v => _settings.NoCeiling = v);

      var verticalPortalRow = _ui.AddToggle(page, "Portal mode: top and bottom",
          () => _settings.WrapFloor,
          v => _settings.WrapFloor = _settings.WrapCeiling = v);
      _bindings.Add(() => verticalPortalRow.SetActive(
          _settings.NoFloor || _settings.NoCeiling));

      var horizontalPortalRow = _ui.AddToggle(page, "Portal mode: left and right",
          () => _settings.WrapWall,
          v => _settings.WrapWall = v);
      _bindings.Add(() => horizontalPortalRow.SetActive(_settings.NoWall));

      _ui.AddToggle(page, "Pets ride planets",
          () => _settings.RidePlanets,
          v => _settings.RidePlanets = v);

      _ui.AddSlider(page, "Panic distance", 0f, () => 4f, false, "x",
          () => _settings.PanicRadius,
          v => _settings.PanicRadius = v);

      _ui.AddToggle(page, "Falling pets steer toward the nearest landing spot",
          () => _settings.FallSteer,
          v => _settings.FallSteer = v);
      var steerRow = _ui.AddSlider(page, "Pull strength", 0.1f, () => 3f, false, "x",
          () => _settings.FallSteerScale,
          v => _settings.FallSteerScale = v, 24);
      _bindings.Add(() => steerRow.SetActive(_settings.FallSteer));

#if DEV
      _ui.AddToggle(page, "Developer: show hitboxes (tile tops, decoration surfaces, planet circles, pets)",
          () => _settings.ShowHitboxes,
          v => _settings.ShowHitboxes = v);
      _ui.AddHelp(page, "Pets draw two boxes: a yellow square — the area the mouse can " +
          "grab them by, dim while they can't be picked up — and a collision circle: " +
          "orange gets pushed around, red is an immovable wall, grey passes through");
#endif
  }


  public void OnShown() => Refresh();
  public void Refresh() => _bindings?.Refresh();
  public void Dispose() { }
}
