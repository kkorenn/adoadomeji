using System;
using System.Globalization;
using System.Linq;

namespace Adomeji.UpdateEngine;

internal sealed class SemanticVersion : IComparable<SemanticVersion>
{
  private readonly int[] _core;
  private readonly string[] _prerelease;
  private SemanticVersion(int[] core, string[] prerelease) { _core = core; _prerelease = prerelease; }
  public static SemanticVersion Parse(string value)
  { if (!TryParse(value, out SemanticVersion version)) throw new FormatException("Invalid semantic version: " + value); return version; }

  public static bool TryParse(string value, out SemanticVersion version)
  {
    version = null;
    if (string.IsNullOrWhiteSpace(value)) return false;
    string normalized = value.Trim().TrimStart('v', 'V');
    int build = normalized.IndexOf('+'); if (build >= 0) normalized = normalized.Substring(0, build);
    string prereleaseText = null;
    int dash = normalized.IndexOf('-');
    if (dash >= 0) { prereleaseText = normalized.Substring(dash + 1); normalized = normalized.Substring(0, dash); }
    string[] parts = normalized.Split('.');
    if (parts.Length < 1 || parts.Length > 4) return false;
    int[] core = new int[Math.Max(3, parts.Length)];
    for (int i = 0; i < parts.Length; i++)
      if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out core[i]) || core[i] < 0) return false;
    string[] prerelease = Array.Empty<string>();
    if (prereleaseText != null)
    {
      prerelease = prereleaseText.Split('.');
      if (prerelease.Length == 0 || prerelease.Any(x => string.IsNullOrEmpty(x) || x.Any(c => !char.IsLetterOrDigit(c) && c != '-'))) return false;
    }
    version = new SemanticVersion(core, prerelease); return true;
  }

  public int CompareTo(SemanticVersion other)
  {
    if (other == null) return 1;
    int count = Math.Max(_core.Length, other._core.Length);
    for (int i = 0; i < count; i++)
    {
      int comparison = (i < _core.Length ? _core[i] : 0).CompareTo(i < other._core.Length ? other._core[i] : 0);
      if (comparison != 0) return comparison;
    }
    if (_prerelease.Length == 0 || other._prerelease.Length == 0)
      return _prerelease.Length == other._prerelease.Length ? 0 : (_prerelease.Length == 0 ? 1 : -1);
    int length = Math.Min(_prerelease.Length, other._prerelease.Length);
    for (int i = 0; i < length; i++)
    {
      int comparison = CompareIdentifier(_prerelease[i], other._prerelease[i]);
      if (comparison != 0) return comparison;
    }
    return _prerelease.Length.CompareTo(other._prerelease.Length);
  }

  private static int CompareIdentifier(string left, string right)
  {
    bool ln = left.All(char.IsDigit), rn = right.All(char.IsDigit);
    if (ln && rn)
    {
      string l = left.TrimStart('0'), r = right.TrimStart('0');
      int c = l.Length.CompareTo(r.Length); return c != 0 ? c : string.CompareOrdinal(l, r);
    }
    if (ln != rn) return ln ? -1 : 1;
    return string.CompareOrdinal(left, right);
  }

  public override string ToString() => string.Join(".", _core.Select(x => x.ToString(CultureInfo.InvariantCulture))) +
    (_prerelease.Length == 0 ? "" : "-" + string.Join(".", _prerelease));
}
