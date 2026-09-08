namespace UnityEngine
{
  internal enum KeyCode
  {
    None = 0,
    A = 1,
    F8 = 2,
    Escape = 3,
    Mouse0 = 100,
    Mouse1 = 101,
    Mouse2 = 102,
    Mouse3 = 103,
    Mouse4 = 104,
    Mouse5 = 105,
    Mouse6 = 106,
  }

  internal static class Mathf
  {
    public static int Clamp(int value, int minimum, int maximum) =>
      value < minimum ? minimum : value > maximum ? maximum : value;

    public static float Clamp(float value, float minimum, float maximum) =>
      value < minimum ? minimum : value > maximum ? maximum : value;
  }
}

namespace UnityEngine.UI
{
  internal sealed class Text
  {
    public string text;
  }
}

namespace UnityModManagerNet
{
  public static class UnityModManager
  {
    public sealed class ModEntry { }

    public abstract class ModSettings
    {
      public virtual void Save(ModEntry modEntry) { }
      protected static void Save<T>(T settings, ModEntry modEntry) { }
    }
  }
}

namespace Adomeji.Sprites.Loading
{
  internal static class PlaceholderArt
  {
    public const string DefaultPack = "ELLIE";
  }
}

namespace Adomeji.Pets.Runtime
{
  internal sealed class UnityPetFlockFactory : IPetFlockFactory
  {
    public IPetFlock Create() => throw new System.InvalidOperationException("Use an injected test factory.");
  }
}
