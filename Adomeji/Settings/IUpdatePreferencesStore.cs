namespace Adomeji.Settings;

internal interface IUpdatePreferencesStore
{
  bool ReceiveBetaUpdates { get; set; }
  void Save();
}
