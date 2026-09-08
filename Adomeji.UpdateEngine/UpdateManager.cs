using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Adomeji.UpdateEngine;

internal sealed class UpdateManager
{
  private const string StableReleaseBaseUrl = "https://github.com/KGH1113/adomeji/releases/latest/download/";
  private const string ReleasesApiUrl = "https://api.github.com/repos/KGH1113/adomeji/releases?per_page=20";
  internal const string ManifestAsset = "Adomeji.update.json";
  internal const string PackageAsset = "Adomeji.zip";
  internal const long MaximumPackageBytes = 128L * 1024 * 1024;
  private const long MaximumExtractedBytes = 256L * 1024 * 1024;
  private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(20);
  private static readonly Regex VersionPattern = new Regex(
    "\\\"Version\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.CultureInvariant);

  private readonly string _installPath;
  private readonly string _runtimeRoot;
  private readonly string _versionsRoot;
  private readonly string _preferencesPath;
  private readonly string _stableReleaseBaseUrl;
  private readonly string _releasesApiUrl;
  private readonly bool _allowLocalTestUrls;

  public UpdateManager(string installPath) : this(installPath, StableReleaseBaseUrl, ReleasesApiUrl, false) { }
  internal UpdateManager(string installPath, string stableReleaseBaseUrl, string releasesApiUrl, bool allowLocalTestUrls = true)
  {
    _installPath = Path.GetFullPath(installPath ?? throw new ArgumentNullException(nameof(installPath)));
    _runtimeRoot = Path.Combine(_installPath, "Runtime");
    _versionsRoot = Path.Combine(_runtimeRoot, "versions");
    _preferencesPath = Path.Combine(_installPath, "UpdateSettings.json");
    _stableReleaseBaseUrl = stableReleaseBaseUrl;
    _releasesApiUrl = releasesApiUrl;
    _allowLocalTestUrls = allowLocalTestUrls;
  }

  public UpdateResult Resolve(string currentVersion)
  {
    CleanupTemporaryArtifacts();
    using CancellationTokenSource timeout = new CancellationTokenSource(NetworkTimeout);
    Task<UpdateResult> operation = ResolveAsync(currentVersion, timeout.Token);
    Task deadline = Task.Delay(NetworkTimeout);
    if (Task.WhenAny(operation, deadline).GetAwaiter().GetResult() != operation)
    {
      timeout.Cancel();
      _ = operation.ContinueWith(t => { if (t.IsFaulted) _ = t.Exception; }, CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
      throw new TimeoutException("Adomeji update operations timed out after 20 seconds.");
    }
    return operation.GetAwaiter().GetResult();
  }

  private async Task<UpdateResult> ResolveAsync(string currentVersion, CancellationToken cancellationToken)
  {
    SemanticVersion current = SemanticVersion.Parse(currentVersion);
    using HttpClient client = CreateClient();
    ReleaseAssets release = await ResolveReleaseAsync(client, cancellationToken).ConfigureAwait(false);
    ReleaseManifest manifest = ReleaseManifest.Parse(await DownloadTextAsync(client, release.ManifestUrl, 16 * 1024, cancellationToken).ConfigureAwait(false));
    SemanticVersion available = SemanticVersion.Parse(manifest.Version);
    if (release.ExpectedVersion != null && available.CompareTo(SemanticVersion.Parse(release.ExpectedVersion)) != 0)
      throw new InvalidDataException("Release tag and manifest versions differ.");
    if (available.CompareTo(current) <= 0) return new UpdateResult { Outcome = UpdateOutcomes.None };

    if (TryResolveExisting(manifest.Version, out UpdateResult reused)) return reused;

    Directory.CreateDirectory(_runtimeRoot);
    string packagePath = Path.Combine(_runtimeRoot, "download-" + Guid.NewGuid().ToString("N") + ".zip");
    try
    {
      await DownloadFileAsync(client, release.PackageUrl, packagePath, manifest.PackageBytes, cancellationToken).ConfigureAwait(false);
      VerifyChecksum(packagePath, manifest.PackageSha256);
      return Candidate(manifest.Version, InstallPackage(packagePath, manifest));
    }
    finally { TryDeleteFile(packagePath); }
  }

  private static UpdateResult Candidate(string version, string path) => new UpdateResult {
    Outcome = UpdateOutcomes.Candidate, Version = version, RuntimePath = path
  };

  internal bool TryResolveExisting(string version, out UpdateResult result)
  {
    string existing = GetVersionDirectory(version);
    if (TryValidateRuntime(existing, version)) { result = Candidate(version, existing); return true; }
    result = null; return false;
  }

  private async Task<ReleaseAssets> ResolveReleaseAsync(HttpClient client, CancellationToken token)
  {
    if (!UpdatePreferences.Load(_preferencesPath).ReceiveBetaUpdates)
      return new ReleaseAssets(null, _stableReleaseBaseUrl + ManifestAsset, _stableReleaseBaseUrl + PackageAsset);

    string response = await DownloadTextAsync(client, _releasesApiUrl, 4 * 1024 * 1024, token).ConfigureAwait(false);
    ReleaseAssets selected = null;
    SemanticVersion selectedVersion = null;
    foreach (JObject release in JArray.Parse(response).OfType<JObject>())
    {
      if (release.Value<bool?>("draft") == true) continue;
      string tag = release.Value<string>("tag_name");
      if (!SemanticVersion.TryParse(tag, out SemanticVersion version)) continue;
      ReleaseAssets assets = ReadReleaseAssets(tag, release["assets"] as JArray);
      if (assets == null || selectedVersion != null && version.CompareTo(selectedVersion) <= 0) continue;
      selected = assets; selectedVersion = version;
    }
    return selected ?? throw new InvalidDataException("No Adomeji release contains both required update assets.");
  }

  private ReleaseAssets ReadReleaseAssets(string version, JArray assets)
  {
    string manifestUrl = null, packageUrl = null;
    foreach (JObject asset in assets?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
    {
      string name = asset.Value<string>("name"), url = asset.Value<string>("browser_download_url");
      if (!IsTrustedReleaseUrl(url)) continue;
      if (name == ManifestAsset) manifestUrl = url;
      else if (name == PackageAsset) packageUrl = url;
    }
    return manifestUrl != null && packageUrl != null ? new ReleaseAssets(version, manifestUrl, packageUrl) : null;
  }

  private string InstallPackage(string packagePath, ReleaseManifest manifest)
  {
    string extractionRoot = Path.Combine(_runtimeRoot, "extract-" + Guid.NewGuid().ToString("N"));
    string target = GetVersionDirectory(manifest.Version);
    try
    {
      ExtractPackage(packagePath, extractionRoot);
      string source = ResolveContainedPath(extractionRoot, manifest.RuntimePath);
      ValidateRuntime(source, manifest.Version);
      Directory.CreateDirectory(_versionsRoot);
      if (Directory.Exists(target)) Directory.Delete(target, true);
      Directory.Move(source, target);
      return target;
    }
    finally { TryDeleteDirectory(extractionRoot); }
  }

  internal static void ExtractPackage(string packagePath, string destinationRoot)
  {
    Directory.CreateDirectory(destinationRoot);
    string rootPrefix = EnsureTrailingSeparator(Path.GetFullPath(destinationRoot));
    using FileStream package = File.OpenRead(packagePath);
    using ZipArchive archive = new ZipArchive(package, ZipArchiveMode.Read);
    long extractedBytes = 0;
    foreach (ZipArchiveEntry entry in archive.Entries)
    {
      if (Path.IsPathRooted(entry.FullName) || IsSymbolicLink(entry))
        throw new InvalidDataException("The update package contains an unsafe entry.");
      extractedBytes = checked(extractedBytes + entry.Length);
      if (extractedBytes > MaximumExtractedBytes) throw new InvalidDataException("The extracted package is too large.");
      string destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
      if (!destinationPath.StartsWith(rootPrefix, StringComparison.Ordinal)) throw new InvalidDataException("The package contains an unsafe path.");
      if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(destinationPath); continue; }
      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
      using Stream source = entry.Open();
      using FileStream destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      source.CopyTo(destination);
      RestoreUnixPermissions(entry, destinationPath);
    }
  }

  private string GetVersionDirectory(string version) => Path.Combine(_versionsRoot, SemanticVersion.Parse(version).ToString());
  private static bool TryValidateRuntime(string directory, string version) { try { ValidateRuntime(directory, version); return true; } catch { return false; } }
  private static void ValidateRuntime(string directory, string expectedVersion)
  {
    string assembly = Path.Combine(directory, "Adomeji.dll");
    string engine = Path.Combine(directory, "Adomeji.UpdateEngine.dll");
    string info = Path.Combine(directory, "Info.json");
    if (!File.Exists(assembly) || !File.Exists(engine) || !File.Exists(info))
      throw new InvalidDataException("The package does not contain a complete runtime.");
    Match match = VersionPattern.Match(File.ReadAllText(info));
    if (!match.Success || SemanticVersion.Parse(match.Groups[1].Value).CompareTo(SemanticVersion.Parse(expectedVersion)) != 0)
      throw new InvalidDataException("The packaged runtime version does not match the manifest.");
  }

  internal void CleanupTemporaryArtifacts()
  {
    if (!Directory.Exists(_runtimeRoot)) return;
    foreach (string file in Directory.GetFiles(_runtimeRoot, "download-*.zip")) TryDeleteFile(file);
    foreach (string dir in Directory.GetDirectories(_runtimeRoot, "extract-*")) TryDeleteDirectory(dir);
  }

  private static string ResolveContainedPath(string root, string relativePath)
  {
    string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
    string fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal)) throw new InvalidDataException("Manifest runtime path escapes the package.");
    return fullPath;
  }

  private bool IsTrustedReleaseUrl(string value)
  {
    if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri)) return false;
    if (_allowLocalTestUrls && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp) return true;
    return uri.Scheme == Uri.UriSchemeHttps && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase);
  }

  private static HttpClient CreateClient()
  {
    HttpClient client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Adomeji-AutoUpdater/1.0");
    return client;
  }

  private static async Task<string> DownloadTextAsync(HttpClient client, string url, int maximumBytes, CancellationToken token)
  {
    using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    using Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    using MemoryStream buffer = new MemoryStream();
    await CopyWithLimitAsync(source, buffer, maximumBytes, token).ConfigureAwait(false);
    return Encoding.UTF8.GetString(buffer.ToArray());
  }

  private static async Task DownloadFileAsync(HttpClient client, string url, string path, long expectedBytes, CancellationToken token)
  {
    using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != expectedBytes)
      throw new InvalidDataException("Package size does not match its manifest.");
    using Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    using FileStream destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
    long copied = await CopyWithLimitAsync(source, destination, MaximumPackageBytes, token).ConfigureAwait(false);
    if (copied != expectedBytes) throw new InvalidDataException("Downloaded package size does not match its manifest.");
  }

  private static async Task<long> CopyWithLimitAsync(Stream source, Stream destination, long maximumBytes, CancellationToken token)
  {
    byte[] buffer = new byte[81920]; long total = 0;
    while (true)
    {
      int read = await source.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
      if (read == 0) return total;
      total += read; if (total > maximumBytes) throw new InvalidDataException("Downloaded update asset is too large.");
      await destination.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
    }
  }

  internal static void VerifyChecksum(string path, string expected)
  {
    using SHA256 sha = SHA256.Create(); using FileStream stream = File.OpenRead(path);
    string actual = string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2")));
    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package checksum mismatch.");
  }

  private static bool IsSymbolicLink(ZipArchiveEntry entry) => ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;
  private static string EnsureTrailingSeparator(string path) => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
  private static void RestoreUnixPermissions(ZipArchiveEntry entry, string path)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
    uint mode = (uint)(entry.ExternalAttributes >> 16) & 0x1FF;
    if (mode != 0 && Chmod(path, mode) != 0) throw new IOException("Could not restore file permissions: " + path);
  }
  [DllImport("libc", EntryPoint = "chmod", SetLastError = true)] private static extern int Chmod(string path, uint mode);
  private static void TryDeleteFile(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
  private static void TryDeleteDirectory(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }
}
