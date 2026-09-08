using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Adomeji.Settings;

internal sealed class UpdatePreferencesStore : IUpdatePreferencesStore
{
  private static readonly Regex BetaPattern = new Regex(
    "\\\"ReceiveBetaUpdates\\\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
  private readonly string _path;
  public bool ReceiveBetaUpdates { get; set; }

  private UpdatePreferencesStore(string path, bool receiveBetaUpdates)
  { _path = path; ReceiveBetaUpdates = receiveBetaUpdates; }

  public static UpdatePreferencesStore Load(string installPath)
  {
    string path = Path.Combine(installPath ?? string.Empty, "UpdateSettings.json");
    try
    {
      if (File.Exists(path))
      {
        Match match = BetaPattern.Match(File.ReadAllText(path));
        if (match.Success) return new UpdatePreferencesStore(path,
          string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase));
      }
    }
    catch { }
    return new UpdatePreferencesStore(path, false);
  }

  public void Save()
  {
    string directory = Path.GetDirectoryName(_path);
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    string temporary = _path + ".tmp";
    File.WriteAllText(temporary,
      "{\n  \"ReceiveBetaUpdates\": " + (ReceiveBetaUpdates ? "true" : "false") + "\n}\n", Encoding.UTF8);
    if (File.Exists(_path)) File.Delete(_path);
    File.Move(temporary, _path);
  }
}
