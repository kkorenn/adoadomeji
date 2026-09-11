using System;
using System.Reflection;
using Adomeji.Gameplay.Events;
using Adomeji.Shared.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace Adomeji.Gameplay.Patches;

internal sealed class GameplayPatchInstaller : IDisposable
{
  private const string HarmonyId = "com.impl.adomeji";

  private readonly IGameplayEventSink _events;
  private readonly IModLogger _logger;

  private Harmony _harmony;
  private bool _installed;

  public PatchCapabilities Capabilities { get; } = new PatchCapabilities();

  public GameplayPatchInstaller(
    IGameplayEventSink events,
    IModLogger logger
  )
  {
    _events = events;
    _logger = logger;
  }

  public void Install()
  {
    if (_installed)
      return;

    GameplayPatchBridge.Connect(_events, _logger);

    _harmony = new Harmony(HarmonyId);

    Capabilities.LevelStarted = TryPatchPostfix(
      typeof(scrController),
      "Awake",
      nameof(GameplayPatchBridge.LevelStartedPostfix)
    );

    Capabilities.LevelCleared = TryPatchPostfix(
      typeof(scrController),
      "OnLandOnPortal",
      nameof(GameplayPatchBridge.LevelClearedPostfix)
    );

    Capabilities.LevelFailed = TryPatchPostfix(
      typeof(scrController),
      "FailAction",
      nameof(GameplayPatchBridge.LevelFailedPostfix)
    );

    Capabilities.Judgements = InstallJudgementPatch();

    int release = GameRelease();

    Capabilities.Overpress = TryPatchPostfix(
      typeof(scrHitTextManager),
      "ShowHitText",
      nameof(GameplayPatchBridge.OverpressPostfix),
      release >= MsDiffHitTextRelease
        ? new[] { typeof(HitMargin), typeof(scrPlanet), typeof(float), typeof(int?) }
        : new[] { typeof(HitMargin), typeof(scrPlanet), typeof(float) }
    );

    Capabilities.MouseSuppression = InstallMouseSuppressionPatches();

    _installed = true;
    _logger.Info("[Harmony] Gameplay patches installed for ADOFAI r" + release + ".");
  }

  // r150 added an `int? msDiffNullable` parameter to scrHitTextManager.ShowHitText.
  private const int MsDiffHitTextRelease = 150;

  // releaseNumber is a const, so it must be read by reflection; a direct
  // reference would bake in the release this assembly was compiled against.
  private static int GameRelease()
  {
    Assembly game = typeof(scrController).Assembly;
    foreach (string holder in new[] { "Releases", "GCNS" })
    {
      FieldInfo field = game.GetType(holder)?.GetField(
        "releaseNumber",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
      );
      if (field == null)
        continue;

      object value = field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
      if (value is int release)
        return release;
    }

    return 0;
  }

  public void Uninstall()
  {
    if (!_installed)
      return;

    _harmony?.UnpatchAll(HarmonyId);
    _harmony = null;

    GameplayPatchBridge.Disconnect();

    _installed = false;
    _logger.Info("[Harmony] Gameplay patches removed.");
  }

  private bool InstallJudgementPatch()
  {
    Type[] parameters = { typeof(HitMargin) };

    if (
      TryPatchPostfix(
        typeof(scrMarginTracker),
        "AddHit",
        nameof(GameplayPatchBridge.JudgementPostfix),
        parameters
      )
    )
    {
      return true;
    }

    return TryPatchPostfix(
      typeof(scrMistakesManager),
      "AddHit",
      nameof(GameplayPatchBridge.JudgementPostfix),
      parameters
    );
  }

  private bool InstallMouseSuppressionPatches()
  {
    bool installed = false;
    installed |= TryPatchPrefix(
      typeof(scnEditor),
      "HandleMouseActions",
      nameof(GameplayPatchBridge.SkipWhileMouseSuppressed)
    );
    installed |= TryPatchPrefix(
      typeof(scnEditor),
      "DragCamera",
      nameof(GameplayPatchBridge.SkipWhileMouseSuppressed),
      new[] { typeof(Vector3) }
    );

    Type inputSource = AccessTools.TypeByName(
      "Rewired.Integration.UnityUI.RewiredPointerInputModule+UnityInputSource"
    );
    if (inputSource != null)
    {
      string[] names = { "GetButton", "GetButtonDown", "GetButtonUp" };
      foreach (string name in names)
      {
        installed |= TryPatchPostfix(
          inputSource,
          "Rewired.UI.IMouseInputSource." + name,
          nameof(GameplayPatchBridge.MouseButtonPostfix),
          new[] { typeof(int) }
        );
      }
    }

    installed |= TryPatchPostfix(
      AccessTools.TypeByName("Rewired.Integration.UnityUI.RewiredStandaloneInputModule"),
      "GetMouseButtonDownOnAnyMouse",
      nameof(GameplayPatchBridge.MouseButtonPostfix),
      new[] { typeof(int) }
    );
    return installed;
  }

  private bool TryPatchPrefix(
    Type targetType,
    string targetMethod,
    string patchMethod,
    Type[] parameters = null
  )
  {
    return TryPatch(
      targetType,
      targetMethod,
      patchMethod,
      parameters,
      true
    );
  }

  private bool TryPatchPostfix(
    Type targetType,
    string targetMethod,
    string patchMethod,
    Type[] parameters = null
  )
  {
    return TryPatch(
      targetType,
      targetMethod,
      patchMethod,
      parameters,
      false
    );
  }

  private bool TryPatch(
    Type targetType,
    string targetMethod,
    string patchMethod,
    Type[] parameters,
    bool prefix
  )
  {
    if (targetType == null)
    {
      _logger.Error("[Harmony] Type not found for method: " + targetMethod);
      return false;
    }

    try
    {
      MethodInfo original = parameters == null
        ? AccessTools.Method(targetType, targetMethod)
        : AccessTools.Method(targetType, targetMethod, parameters);

      if (original == null)
      {
        _logger.Error(
          "[Harmony] Method not found: "
          + targetType.FullName
          + "."
          + targetMethod
        );
        return false;
      }

      MethodInfo callback = AccessTools.Method(typeof(GameplayPatchBridge), patchMethod);
      if (callback == null)
        throw new MissingMethodException(typeof(GameplayPatchBridge).FullName, patchMethod);

      var harmonyMethod = new HarmonyMethod(callback);
      _harmony.Patch(
        original,
        prefix: prefix ? harmonyMethod : null,
        postfix: prefix ? null : harmonyMethod
      );
      return true;
    }
    catch (Exception exception)
    {
      _logger.Error("Harmony " + targetType.FullName + "." + targetMethod, exception);
      return false;
    }
  }

  public void Dispose()
  {
    Uninstall();
  }
}
