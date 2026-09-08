using System.Collections.Generic;
using Adomeji.Sprites.Loading;
using UnityModManagerNet;

namespace Adomeji.Settings;

public sealed class AdomejiSettings : UnityModManager.ModSettings
{
  public const int CurrentSchemaVersion = 1;

  public int SchemaVersion = CurrentSchemaVersion;
  public int Count = 3;
  public bool RaisePetLimit;
  public float Scale = 1f;
  public bool UncapScale;
  public float Speed = 1f;
  public bool FallSteer = true;
  public float FallSteerScale = 1f;
  public bool AllowDrag = true;
  public bool OptimizedMode;
  public List<string> SpritePacks = new List<string> { PlaceholderArt.DefaultPack };
  public List<float> PackWeights = new List<float> { 1f };
  public bool FilterPets = true;
  public bool SwallowMouse = true;
  public bool NoFloor;
  public bool NoWall;
  public bool NoCeiling;
  public bool WrapFloor = true;
  public bool WrapWall = true;
  public bool WrapCeiling = true;
  public bool RidePlanets = true;
  public float PanicRadius = 1f;
  public bool PetCollisionsTiles = true;
  public bool PetCollisionsScreen = true;
  public bool ZoomScale = true;
  public bool ShowHitboxes;
  public float UiScale = 1f;
  public string AccentHex = "FF6B52";
  public string ToggleKey = "F8";

  public int MaxPets => RaisePetLimit ? 100 : 8;
  public float MaxScale => UncapScale ? 20f : 3f;

  public void Normalize()
  {
    Count = UnityEngine.Mathf.Clamp(Count, 1, MaxPets);
    Scale = UnityEngine.Mathf.Clamp(Scale, 0.5f, MaxScale);
    Speed = UnityEngine.Mathf.Clamp(Speed, 0.5f, 2.5f);
    FallSteerScale = UnityEngine.Mathf.Clamp(FallSteerScale, 0.1f, 3f);
    PanicRadius = UnityEngine.Mathf.Clamp(PanicRadius, 0f, 4f);
    UiScale = UnityEngine.Mathf.Clamp(UiScale, 0.6f, 1.6f);
    AccentHex = string.IsNullOrWhiteSpace(AccentHex) ? "FF6B52" : AccentHex;
    ToggleKey = string.IsNullOrWhiteSpace(ToggleKey) ? "F8" : ToggleKey;

    if (SpritePacks == null)
      SpritePacks = new List<string>();

    SpritePacks.RemoveAll(string.IsNullOrWhiteSpace);
    if (SpritePacks.Count == 0)
      SpritePacks.Add(PlaceholderArt.DefaultPack);

    SyncPackWeights();
    WrapCeiling = WrapFloor;
    SchemaVersion = CurrentSchemaVersion;
  }

  public void SyncPackWeights()
  {
    if (PackWeights == null)
      PackWeights = new List<float>();

    while (PackWeights.Count > SpritePacks.Count)
      PackWeights.RemoveAt(PackWeights.Count - 1);

    while (PackWeights.Count < SpritePacks.Count)
      PackWeights.Add(1f);

    for (int index = 0; index < PackWeights.Count; index++)
    {
      if (!(PackWeights[index] > 0f))
        PackWeights[index] = 1f;
    }
  }

  public override void Save(UnityModManager.ModEntry modEntry)
  {
    Normalize();
    Save(this, modEntry);
  }
}
