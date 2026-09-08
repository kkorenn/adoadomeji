using System;
using Adomeji.Composition;
using Adomeji.Pets.Runtime;
using UnityEngine;
using UnityModManagerNet;

namespace Adomeji;

public static class Main
{
  private static AdomejiRuntime _runtime;

  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    try
    {
      _runtime = AdomejiRuntime.Create(modEntry);

      modEntry.OnToggle = OnToggle;
      modEntry.OnUnload = OnUnload;
      modEntry.OnUpdate = OnUpdate;
      modEntry.OnGUI = OnGUI;
      modEntry.OnSaveGUI = OnSaveGUI;

      modEntry.Logger.Log("Adomeji loaded.");
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }

  private static bool OnToggle(
    UnityModManager.ModEntry modEntry,
    bool enabled
  )
  {
    try
    {
      _runtime.SetEnabled(enabled);
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }

  private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
  {
    // UMM's callback delta follows the game's timeScale. ADOFAI sets it to
    // zero while leaving a level and on result/menu screens, but desktop pets
    // must keep animating there, so simulation time is explicitly unscaled.
    float simulationDelta = SimulationFrameDelta.Resolve(deltaTime, Time.unscaledDeltaTime);
    _runtime?.Tick(simulationDelta);
  }

  private static void OnGUI(UnityModManager.ModEntry modEntry)
  {
    if (GUILayout.Button("Open Adomeji settings", GUILayout.Width(220f)))
    {
      _runtime?.ShowSettings();
      UnityModManager.UI.Instance?.ToggleWindow(false);
    }

    string key = _runtime?.Settings.ToggleKey ?? "None";
    GUILayout.Label(key == "None" ? "No toggle key bound" : "Press [" + key + "] in-game");
  }

  private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
  {
    _runtime?.SaveSettings();
  }

  private static bool OnUnload(
    UnityModManager.ModEntry modEntry
  )
  {
    try
    {
      _runtime?.Dispose();
      _runtime = null;
      return true;
    }
    catch (Exception exception)
    {
      modEntry.Logger.Error(exception.ToString());
      return false;
    }
  }
}
