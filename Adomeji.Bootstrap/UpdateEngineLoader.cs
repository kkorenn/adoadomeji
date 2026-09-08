using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityModManagerNet;

namespace Adomeji.Bootstrap;

internal static class UpdateEngineLoader
{
  private const string EntryType = "Adomeji.UpdateEngine.EntryPoint";

  public static UpdateResolution Resolve(UnityModManager.ModEntry modEntry, RuntimeCandidate current)
  {
    string enginePath = current.UpdateEnginePath;
    if (!File.Exists(enginePath)) throw new FileNotFoundException("The Adomeji update engine is missing.", enginePath);
    Assembly assembly = Assembly.LoadFrom(enginePath);
    Type type = assembly.GetType(EntryType, true);
    MethodInfo method = type.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static, null,
      new[] { typeof(UnityModManager.ModEntry), typeof(string) }, null)
      ?? throw new MissingMethodException(EntryType, "Resolve");
    string request = JsonConvert.SerializeObject(new { InstallPath = modEntry.Path, CurrentVersion = current.Version });
    try { return UpdateResolution.Parse(method.Invoke(null, new object[] { modEntry, request }) as string); }
    catch (TargetInvocationException exception) when (exception.InnerException != null) { throw exception.InnerException; }
  }
}

internal sealed class UpdateResolution
{
  public bool HasCandidate { get; private set; }
  public string Version { get; private set; }
  public string RuntimePath { get; private set; }
  public static UpdateResolution None() => new UpdateResolution();

  public static UpdateResolution Parse(string json)
  {
    JObject root = JObject.Parse(json ?? throw new InvalidDataException("The update engine returned no result."));
    string outcome = root.Value<string>("Outcome") ?? root.Value<string>("outcome");
    if (outcome == "none" || outcome == "error") return None();
    if (outcome != "candidate") throw new InvalidDataException("The update engine returned an invalid outcome.");
    return new UpdateResolution {
      HasCandidate = true,
      Version = root.Value<string>("Version") ?? root.Value<string>("version"),
      RuntimePath = root.Value<string>("RuntimePath") ?? root.Value<string>("runtimePath")
    };
  }
}
