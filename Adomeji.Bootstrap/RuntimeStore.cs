using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Adomeji.Bootstrap;

internal sealed class RuntimeStore
{
  private static readonly Regex VersionPattern = new Regex(
    "\\\"Version\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.CultureInvariant);
  private readonly string _runtimeRoot;
  private readonly string _versionsRoot;
  private readonly string _statePath;
  private readonly string _installPath;

  public RuntimeStore(string installPath)
  {
    _installPath = Path.GetFullPath(installPath);
    _runtimeRoot = Path.Combine(_installPath, "Runtime");
    _versionsRoot = Path.Combine(_runtimeRoot, "versions");
    _statePath = Path.Combine(_runtimeRoot, "state.json");
  }

  public RuntimeState LoadAndRepair()
  {
    string backup = _statePath + ".bak";
    if (!File.Exists(_statePath) && File.Exists(backup)) File.Move(backup, _statePath);
    if (!File.Exists(_statePath))
      throw new InvalidDataException("Adomeji Runtime/state.json is missing. Install the first bootstrap release manually.");
    RuntimeState state = JsonConvert.DeserializeObject<RuntimeState>(File.ReadAllText(_statePath));
    if (state == null || state.SchemaVersion != 1 || string.IsNullOrWhiteSpace(state.Current))
      throw new InvalidDataException("Adomeji runtime state is invalid.");

    // A trial left behind means the game terminated before promotion.
    if (!string.IsNullOrWhiteSpace(state.Trial))
    {
      DeleteUnreferencedRuntime(state.Trial, state);
      state.Trial = null;
      Save(state);
    }
    try { GetCandidate(state.Current); }
    catch when (!string.IsNullOrWhiteSpace(state.Previous))
    {
      GetCandidate(state.Previous);
      state.Current = state.Previous;
      state.Previous = null;
      Save(state);
    }
    CleanupVersions(state);
    TryDeleteFile(Path.Combine(_installPath, "Adomeji.dll"));
    TryDeleteFile(Path.Combine(_installPath, "Adomeji.pdb"));
    return state;
  }

  public RuntimeCandidate GetCandidate(string version)
  {
    if (string.IsNullOrWhiteSpace(version)) throw new InvalidDataException("The runtime version is missing.");
    return ValidateCandidate(version, Path.Combine(_versionsRoot, NormalizeVersion(version)));
  }

  public RuntimeCandidate ValidateCandidate(string version, string runtimePath)
  {
    string expected = Path.GetFullPath(Path.Combine(_versionsRoot, NormalizeVersion(version)));
    string actual = Path.GetFullPath(runtimePath ?? string.Empty);
    if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
      throw new InvalidDataException("The update engine returned an unexpected runtime path.");
    string assembly = Path.Combine(actual, "Adomeji.dll");
    string engine = Path.Combine(actual, "Adomeji.UpdateEngine.dll");
    string info = Path.Combine(actual, "Info.json");
    if (!File.Exists(assembly) || !File.Exists(engine) || !File.Exists(info)) throw new InvalidDataException("The runtime is incomplete.");
    Match match = VersionPattern.Match(File.ReadAllText(info));
    if (!match.Success || !VersionsEqual(match.Groups[1].Value, version))
      throw new InvalidDataException("The runtime Info.json version is invalid.");
    return new RuntimeCandidate(NormalizeVersion(version), actual);
  }

  public void Promote(RuntimeState state, string version)
  {
    string normalized = NormalizeVersion(version);
    if (!VersionsEqual(state.Current, normalized)) state.Previous = state.Current;
    state.Current = normalized;
    state.Trial = null;
    Save(state);
    CleanupVersions(state);
  }

  public void Save(RuntimeState state)
  {
    Directory.CreateDirectory(_runtimeRoot);
    string temporary = _statePath + ".tmp";
    string backup = _statePath + ".bak";
    File.WriteAllText(temporary, JsonConvert.SerializeObject(state, Formatting.Indented) + Environment.NewLine, Encoding.UTF8);
    if (File.Exists(_statePath))
    {
      if (File.Exists(backup)) File.Delete(backup);
      File.Replace(temporary, _statePath, backup, true);
      TryDeleteFile(backup);
    }
    else File.Move(temporary, _statePath);
  }

  public void DeleteUnreferencedRuntime(string version, RuntimeState state)
  {
    if (string.IsNullOrWhiteSpace(version) || VersionsEqual(version, state.Current) || VersionsEqual(version, state.Previous)) return;
    TryDeleteDirectory(Path.Combine(_versionsRoot, NormalizeVersion(version)));
  }

  private void CleanupVersions(RuntimeState state)
  {
    if (!Directory.Exists(_versionsRoot)) return;
    foreach (string directory in Directory.GetDirectories(_versionsRoot))
    {
      string name = Path.GetFileName(directory);
      if (VersionsEqual(name, state.Current) || VersionsEqual(name, state.Previous) || VersionsEqual(name, state.Trial)) continue;
      TryDeleteDirectory(directory);
    }
  }

  private static string NormalizeVersion(string version)
  {
    string normalized = version.Trim().TrimStart('v', 'V');
    if (normalized.Length == 0 || normalized.Any(c => !(char.IsLetterOrDigit(c) || c == '.' || c == '-')))
      throw new InvalidDataException("The runtime version is invalid.");
    return normalized;
  }

  private static bool VersionsEqual(string left, string right) => left != null && right != null &&
    string.Equals(NormalizeVersion(left), NormalizeVersion(right), StringComparison.OrdinalIgnoreCase);
  private static void TryDeleteDirectory(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }
  private static void TryDeleteFile(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}
