using System;

namespace Adomeji.Sprites.Configuration;

internal enum SpriteRoleEntryKind
{
  Role,
  PingPong,
  Behavior,
}

internal readonly struct SpriteRoleEntry
{
  public SpriteRoleEntryKind Kind { get; }
  public string Key { get; }
  public string Value { get; }
  public bool Enabled { get; }

  public SpriteRoleEntry(SpriteRoleEntryKind kind, string key, string value, bool enabled)
  {
    Kind = kind;
    Key = key;
    Value = value;
    Enabled = enabled;
  }
}

internal static class SpriteRoleEntryParser
{
  private const string BehaviorPrefix = "behavior.";

  public static bool TryParse(string raw, Func<string, bool> isValidRole, out SpriteRoleEntry entry)
  {
    entry = default;
    string line = raw.Trim();
    if (line.Length == 0 || line[0] == '#') return false;
    int equals = line.IndexOf('=');
    if (equals <= 0) return false;

    string key = line.Substring(0, equals).Trim();
    string value = line.Substring(equals + 1).Trim().ToLowerInvariant();
    if (key.Length == 0) return false;

    if (value == "true" || value == "false")
    {
      bool enabled = value == "true";
      if (string.Equals(key, "pingpong", StringComparison.OrdinalIgnoreCase))
      {
        entry = new SpriteRoleEntry(SpriteRoleEntryKind.PingPong, key, value, enabled);
        return true;
      }
      if (key.StartsWith(BehaviorPrefix, StringComparison.OrdinalIgnoreCase)
          && key.Length > BehaviorPrefix.Length)
      {
        entry = new SpriteRoleEntry(
          SpriteRoleEntryKind.Behavior,
          key.Substring(BehaviorPrefix.Length).ToLowerInvariant(),
          value,
          enabled);
        return true;
      }
      return false;
    }

    if (!isValidRole(value)) return false;
    entry = new SpriteRoleEntry(SpriteRoleEntryKind.Role, key, value, false);
    return true;
  }
}
