using UnityModManagerNet;

namespace Adomeji.Settings;

internal interface ISettingsStore
{
  AdomejiSettings Current { get; }
  void Save();
}

internal sealed class SettingsStore : ISettingsStore
{
  private readonly UnityModManager.ModEntry _modEntry;

  private SettingsStore(UnityModManager.ModEntry modEntry, AdomejiSettings current)
  {
    _modEntry = modEntry;
    Current = current;
  }

  public AdomejiSettings Current { get; }

  public static SettingsStore Load(UnityModManager.ModEntry modEntry)
  {
    AdomejiSettings settings = UnityModManager.ModSettings.Load<AdomejiSettings>(modEntry);
    if (settings == null || settings.SchemaVersion != AdomejiSettings.CurrentSchemaVersion)
      settings = new AdomejiSettings();

    settings.Normalize();
    return new SettingsStore(modEntry, settings);
  }

  public void Save()
  {
    Current.Save(_modEntry);
  }
}
