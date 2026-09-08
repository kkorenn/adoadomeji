using System;
using UnityModManagerNet;

namespace Adomeji.Bootstrap;

public static class Bootstrap
{
  private const string PayloadEntryMethod = "Adomeji.Main.Load";

  public static bool Load(UnityModManager.ModEntry modEntry)
  {
    string displayName = modEntry.Info.DisplayName;
    RuntimeStore store = new RuntimeStore(modEntry.Path);
    try
    {
      RuntimeState state = store.LoadAndRepair();
      RuntimeCandidate current = store.GetCandidate(state.Current);
      modEntry.Info.Version = current.Version;
      modEntry.Info.DisplayName = modEntry.Info.Id + " <color=grey>[Checking for updates...]</color>";
      UpdateResolution resolution;
      try { resolution = UpdateEngineLoader.Resolve(modEntry, current); }
      catch (Exception exception)
      {
        Warn(modEntry, "Update check failed. Loading the current runtime.", exception);
        resolution = UpdateResolution.None();
      }
      modEntry.Info.DisplayName = displayName;
      if (!resolution.HasCandidate) return TryLoad(modEntry, current, out _);

      RuntimeCandidate trial = store.ValidateCandidate(resolution.Version, resolution.RuntimePath);
      state.Trial = trial.Version;
      store.Save(state);
      modEntry.Info.Version = trial.Version;
      if (TryLoad(modEntry, trial, out Exception loadException))
      {
        try { store.Promote(state, trial.Version); }
        catch (Exception exception) { Warn(modEntry, "The updated runtime loaded, but promotion failed.", exception); }
        return true;
      }

      state.Trial = null;
      store.Save(state);
      store.DeleteUnreferencedRuntime(trial.Version, state);
      modEntry.Info.Version = current.Version;
      modEntry.Info.DisplayName = displayName + " <color=red>[Update failed]</color>";
      Warn(modEntry, "The updated runtime failed. The previous runtime will load next launch.", loadException);
      return false;
    }
    catch (Exception exception)
    {
      modEntry.Info.DisplayName = displayName + " <color=red>[Load failed]</color>";
      Warn(modEntry, "The runtime launcher failed.", exception);
      return false;
    }
  }

  private static bool TryLoad(UnityModManager.ModEntry modEntry, RuntimeCandidate candidate, out Exception exception)
  {
    try { PayloadLoader.Load(candidate.AssemblyPath, PayloadEntryMethod, modEntry); exception = null; return true; }
    catch (Exception caught) { exception = caught; return false; }
  }

  private static void Warn(UnityModManager.ModEntry modEntry, string message, Exception exception)
  {
    modEntry.Logger.Warning("[AutoUpdate] " + message);
    if (exception != null) modEntry.Logger.Warning("[AutoUpdate] " + exception);
  }
}
