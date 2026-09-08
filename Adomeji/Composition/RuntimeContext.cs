using System.IO;
using System.Reflection;
using Adomeji.Settings;
using Adomeji.Shared.Diagnostics;

namespace Adomeji.Composition;

internal static class RuntimeContext
{
  private static ISettingsStore _settings;
  private static IModLogger _logger;

  public static AdomejiSettings Settings => _settings.Current;
  public static string ModPath { get; private set; }
  public static string Version { get; private set; }
  public static string SpritesPath => Path.Combine(ModPath, "Sprites");
  public static string BundledSpritesPath => Path.Combine(
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "Sprites");

  public static void Initialize(
    ISettingsStore settings,
    IModLogger logger,
    string modPath,
    string version)
  {
    _settings = settings;
    _logger = logger;
    ModPath = modPath ?? "";
    Version = version ?? "";
  }

  public static void Log(string message) => _logger?.Info(message);

  public static void Clear()
  {
    _settings = null;
    _logger = null;
    ModPath = "";
    Version = "";
  }
}
