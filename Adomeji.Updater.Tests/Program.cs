using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Adomeji.Bootstrap;
using Adomeji.UpdateEngine;

internal static class Program
{
  private static int _tests;
  private static int Main()
  {
    string root = Path.Combine(Path.GetTempPath(), "adomeji-updater-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
      ManifestValidation();
      ChecksumValidation(root);
      UnsafeArchives(root);
      ChannelPreferences(root);
      RuntimeReuse(root);
      RuntimeRepair(root);
      UserDataPreservation(root);
      TemporaryCleanup(root);
      Console.WriteLine("Adomeji.Updater.Tests: " + _tests + " assertions passed.");
      return 0;
    }
    finally { try { Directory.Delete(root, true); } catch { } }
  }

  private static void ManifestValidation()
  {
    Throws<InvalidDataException>(() => ReleaseManifest.Parse("{}"), "invalid manifest");
    Throws<InvalidDataException>(() => ReleaseManifest.Parse(Manifest("1.0.0", 134217729, new string('a', 64), "Adomeji/Runtime")), "oversize manifest");
    Throws<InvalidDataException>(() => ReleaseManifest.Parse(Manifest("1.0.0", 1, "bad", "Adomeji/Runtime")), "bad checksum manifest");
    ReleaseManifest valid = ReleaseManifest.Parse(Manifest("1.2.3", 1, new string('a', 64), "Adomeji/Runtime/versions/1.2.3"));
    Equal("1.2.3", valid.Version, "valid manifest");
  }

  private static void ChecksumValidation(string root)
  {
    string file = Path.Combine(root, "checksum.bin"); File.WriteAllText(file, "payload");
    Throws<InvalidDataException>(() => UpdateManager.VerifyChecksum(file, new string('0', 64)), "checksum mismatch");
    using SHA256 sha = SHA256.Create();
    string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
    UpdateManager.VerifyChecksum(file, hash); True(true, "checksum match");
  }

  private static void UnsafeArchives(string root)
  {
    string zip = Path.Combine(root, "unsafe.zip");
    using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
      archive.CreateEntry("../escape.txt");
    Throws<InvalidDataException>(() => UpdateManager.ExtractPackage(zip, Path.Combine(root, "unsafe-out")), "zip slip rejected");

    string symlink = Path.Combine(root, "symlink.zip");
    using (ZipArchive archive = ZipFile.Open(symlink, ZipArchiveMode.Create))
    {
      ZipArchiveEntry entry = archive.CreateEntry("link"); entry.ExternalAttributes = unchecked((int)0xA1FF0000);
    }
    Throws<InvalidDataException>(() => UpdateManager.ExtractPackage(symlink, Path.Combine(root, "symlink-out")), "symlink rejected");
  }

  private static void ChannelPreferences(string root)
  {
    string path = Path.Combine(root, "prefs.json");
    False(UpdatePreferences.Load(path).ReceiveBetaUpdates, "stable default");
    File.WriteAllText(path, "{\"ReceiveBetaUpdates\":true}");
    True(UpdatePreferences.Load(path).ReceiveBetaUpdates, "beta enabled");
    True(SemanticVersion.Parse("1.0.0-beta.2").CompareTo(SemanticVersion.Parse("1.0.0")) < 0, "stable outranks beta");
  }

  private static void RuntimeReuse(string root)
  {
    string install = Path.Combine(root, "reuse"); MakeRuntime(install, "1.2.3");
    UpdateManager manager = new UpdateManager(install, "http://localhost/", "http://localhost/", true);
    True(manager.TryResolveExisting("1.2.3", out UpdateResult result), "installed runtime reused");
    Equal("candidate", result.Outcome, "reuse outcome");
  }

  private static void RuntimeRepair(string root)
  {
    string abandoned = Path.Combine(root, "abandoned"); MakeRuntime(abandoned, "1.0.0"); MakeRuntime(abandoned, "2.0.0");
    WriteState(abandoned, "1.0.0", null, "2.0.0");
    RuntimeState state = new RuntimeStore(abandoned).LoadAndRepair();
    Equal(null, state.Trial, "abandoned trial cleared");
    False(Directory.Exists(VersionPath(abandoned, "2.0.0")), "abandoned trial deleted");

    string rollback = Path.Combine(root, "rollback"); MakeRuntime(rollback, "1.0.0"); MakeRuntime(rollback, "2.0.0");
    File.Delete(Path.Combine(VersionPath(rollback, "2.0.0"), "Adomeji.dll"));
    WriteState(rollback, "2.0.0", "1.0.0", null);
    RuntimeState repaired = new RuntimeStore(rollback).LoadAndRepair();
    Equal("1.0.0", repaired.Current, "current rolled back");
    Equal(null, repaired.Previous, "previous consumed by rollback");

    string promote = Path.Combine(root, "promote"); MakeRuntime(promote, "1.0.0"); MakeRuntime(promote, "2.0.0");
    RuntimeStore store = new RuntimeStore(promote); RuntimeState promoted = new RuntimeState { Current = "1.0.0" };
    store.Promote(promoted, "2.0.0");
    Equal("2.0.0", promoted.Current, "candidate promoted"); Equal("1.0.0", promoted.Previous, "previous preserved");
    True(Directory.Exists(VersionPath(promote, "1.0.0")), "previous files preserved");
  }

  private static void TemporaryCleanup(string root)
  {
    string install = Path.Combine(root, "cleanup"), runtime = Path.Combine(install, "Runtime"); Directory.CreateDirectory(runtime);
    File.WriteAllText(Path.Combine(runtime, "download-old.zip"), "x"); Directory.CreateDirectory(Path.Combine(runtime, "extract-old"));
    new UpdateManager(install, "http://localhost/", "http://localhost/", true).CleanupTemporaryArtifacts();
    False(File.Exists(Path.Combine(runtime, "download-old.zip")), "download temp removed");
    False(Directory.Exists(Path.Combine(runtime, "extract-old")), "extract temp removed");
  }

  private static void UserDataPreservation(string root)
  {
    string install = Path.Combine(root, "userdata"); MakeRuntime(install, "1.0.0"); WriteState(install, "1.0.0", null, null);
    string sprites = Path.Combine(install, "Sprites", "Custom"); Directory.CreateDirectory(sprites);
    string settings = Path.Combine(install, "Settings.xml"); File.WriteAllText(settings, "settings");
    string updates = Path.Combine(install, "UpdateSettings.json"); File.WriteAllText(updates, "updates");
    string roles = Path.Combine(sprites, "sprite-roles.txt"); File.WriteAllText(roles, "walk0=walk");
    File.WriteAllText(Path.Combine(install, "Adomeji.dll"), "legacy");
    new RuntimeStore(install).LoadAndRepair();
    Equal("settings", File.ReadAllText(settings), "Settings.xml preserved");
    Equal("updates", File.ReadAllText(updates), "UpdateSettings.json preserved");
    Equal("walk0=walk", File.ReadAllText(roles), "external sprite roles preserved");
    False(File.Exists(Path.Combine(install, "Adomeji.dll")), "legacy root payload removed");
  }

  private static void MakeRuntime(string install, string version)
  {
    string path = VersionPath(install, version); Directory.CreateDirectory(path);
    File.WriteAllText(Path.Combine(path, "Adomeji.dll"), "test");
    File.WriteAllText(Path.Combine(path, "Adomeji.UpdateEngine.dll"), "test");
    File.WriteAllText(Path.Combine(path, "Info.json"), "{\"Version\":\"" + version + "\"}");
  }
  private static string VersionPath(string install, string version) => Path.Combine(install, "Runtime", "versions", version);
  private static void WriteState(string install, string current, string previous, string trial)
  {
    string runtime = Path.Combine(install, "Runtime"); Directory.CreateDirectory(runtime);
    File.WriteAllText(Path.Combine(runtime, "state.json"), "{\"SchemaVersion\":1,\"Current\":\"" + current + "\",\"Previous\":" + Json(previous) + ",\"Trial\":" + Json(trial) + "}");
  }
  private static string Json(string value) => value == null ? "null" : "\"" + value + "\"";
  private static string Manifest(string version, long bytes, string sha, string runtime) =>
    "{\"schemaVersion\":1,\"version\":\"" + version + "\",\"packageAsset\":\"Adomeji.zip\",\"packageBytes\":" + bytes + ",\"packageSha256\":\"" + sha + "\",\"runtimePath\":\"" + runtime + "\"}";
  private static void True(bool value, string name) { _tests++; if (!value) throw new InvalidOperationException("FAILED: " + name); }
  private static void False(bool value, string name) => True(!value, name);
  private static void Equal<T>(T expected, T actual, string name) { _tests++; if (!Equals(expected, actual)) throw new InvalidOperationException("FAILED: " + name + "; expected=" + expected + ", actual=" + actual); }
  private static void Throws<T>(Action action, string name) where T : Exception { _tests++; try { action(); } catch (T) { return; } throw new InvalidOperationException("FAILED: " + name); }
}
