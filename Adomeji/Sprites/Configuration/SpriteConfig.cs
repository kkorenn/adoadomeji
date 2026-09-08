using System;
using System.Collections.Generic;
using System.IO;
using Adomeji.Sprites.Loading;
using Adomeji.Shared.Text;
using Adomeji.Composition;

namespace Adomeji.Sprites.Configuration
{
    // Autonomous/reaction features that can be enabled per sprite pack.
    // Falling, collision physics, and dragging stay core so a pet always has
    // a safe way to land and recover even when every optional behavior is off.
    public enum PackBehavior
    {
        Walk,
        Sit,
        Idle,
        Jump,
        Climb,
        Ceiling,
        Edge,
        Wave,
        Panic,
        Bonk,
        Judgement,
        Celebrate,
        Death,
    }

    // Per-pack sprite role overrides, edited from the Sprites tab. Stored as
    // sprite-roles.txt next to the pack's art, so a customized pack can be
    // zipped up and shared with its configuration intact.
    //
    // One line per override: "<file basename>=<role>". A file with no line
    // follows the normal filename rules ("auto"); "disabled" hides the file
    // entirely; any role name reassigns the file to that pose/reaction —
    // several files on one role become that role's animation frames.
    //
    // The same file also carries pack-wide options, written as
    // "<option>=true|false" ("pingpong=false", "behavior.jump=false") — a
    // boolean value is what separates an option line from a role line.
    public static class SpriteConfig
    {
        public const string FileName = "sprite-roles.txt";
        public const string Disabled = "disabled";
        public const string PingPongKey = "pingpong";
        private const string BehaviorPrefix = "behavior.";

        public static readonly string[] BehaviorKeys =
        {
            "walk", "sit", "idle", "jump", "climb", "ceiling", "edge",
            "wave", "panic", "bonk", "judgement", "celebrate", "death",
        };

        // role id per Pose, indexed by (int)Pose — tracks the enum
        public static readonly string[] PoseRoles =
        {
            "walk", "sit", "fall", "happy", "shock", "dead", "bonk",
            "falling", "climb", "ceiling", "jump",
            "panic", "edge", "idle", "wave",
        };

        public static string RoleName(Pose pose) => PoseRoles[(int)pose];

        private static string _dir = "";
        private static readonly Dictionary<string, string> _map =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // pack-wide: walk cycles 1..N..1 instead of looping (default on)
        private static bool _pingPong = true;
        public static bool PingPong => _pingPong;

        private static readonly bool[] _behaviors = new bool[BehaviorKeys.Length];

        public static bool BehaviorEnabled(PackBehavior behavior) =>
            _behaviors[(int)behavior];

        // Load the overrides that live next to the given image folder (called
        // by ApplyPack once the real art folder is known).
        public static void Load(string imageDir)
        {
            _dir = ConfigDirectory(imageDir);
            _map.Clear();
            _pingPong = true;
            for (int i = 0; i < _behaviors.Length; i++) _behaviors[i] = true;
            if (_dir.Length == 0) return;
            try
            {
                string path = Path.Combine(_dir, FileName);
                if (!File.Exists(path)) return;
                bool stale = false;
                foreach (string raw in File.ReadAllLines(path))
                {
                    if (!SpriteRoleEntryParser.TryParse(raw, IsValidRole, out SpriteRoleEntry entry))
                        continue;
                    if (entry.Kind == SpriteRoleEntryKind.PingPong)
                    {
                        _pingPong = entry.Enabled;
                        continue;
                    }
                    if (entry.Kind == SpriteRoleEntryKind.Behavior)
                    {
                        int behavior = Array.IndexOf(BehaviorKeys, entry.Key);
                        if (behavior >= 0) _behaviors[behavior] = entry.Enabled;
                        continue;
                    }
                    // "flip" used to auto-resolve to jump, so packs opened in
                    // an older build got flip0=jump written out as an explicit
                    // override. It's a rude gesture, not a jump: drop the
                    // stale line and rewrite the file.
                    if (entry.Value == "jump" && IsFlipFile(entry.Key)) { stale = true; continue; }
                    _map[entry.Key] = entry.Value;
                }
                if (_map.Count > 0)
                    RuntimeContext.Log("[Sprites] roles: " + _map.Count + " override(s) from " + path);
                if (stale)
                {
                    RuntimeContext.Log("[Sprites] roles: dropped stale flip=jump override(s) in " + path);
                    Save();
                }
            }
            catch (Exception e)
            {
                RuntimeContext.Log("[Sprites] roles: unreadable (" + e.Message + ")");
            }
        }

        private static string ConfigDirectory(string imageDir)
        {
            if (string.IsNullOrEmpty(imageDir)) return "";
            try
            {
                string bundled = Path.GetFullPath(RuntimeContext.BundledSpritesPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string actual = Path.GetFullPath(imageDir);
                if (!actual.StartsWith(bundled, StringComparison.OrdinalIgnoreCase)) return imageDir;
                string relative = actual.Substring(bundled.Length);
                return Path.Combine(RuntimeContext.SpritesPath, relative);
            }
            catch { return imageDir; }
        }

        // flip.png / flip0.png / flip12.png — the gesture, not a pose
        private static bool IsFlipFile(string baseName)
        {
            string stem;
            int n;
            SplitTrailingNumber(baseName, out stem, out n);
            return string.Equals(stem, "flip", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidRole(string role)
        {
            if (role == Disabled) return true;
            foreach (string r in PoseRoles)
                if (r == role) return true;
            return false;
        }

        // Explicitly configured (disabled or reassigned)? Such a file no
        // longer participates in filename-based auto loading.
        public static bool HasExplicit(string baseName) => _map.ContainsKey(baseName);

        // The configured role, or null when the file is on auto.
        public static string RoleOf(string baseName)
        {
            string role;
            return _map.TryGetValue(baseName, out role) ? role : null;
        }

        // Set (or clear, with null/"auto") a file's role and persist.
        public static void SetRole(string baseName, string role)
        {
            if (string.IsNullOrEmpty(role) || role == "auto") _map.Remove(baseName);
            else if (IsValidRole(role)) _map[baseName] = role;
            else return;
            Save();
        }

        // Pack-wide ping-pong walk cycle; persisted with the roles.
        public static void SetPingPong(bool on)
        {
            _pingPong = on;
            Save();
        }

        public static void SetBehavior(PackBehavior behavior, bool on)
        {
            _behaviors[(int)behavior] = on;
            Save();
        }

        private static int BehaviorIndex(string option)
        {
            if (!option.StartsWith(BehaviorPrefix, StringComparison.OrdinalIgnoreCase)) return -1;
            string key = option.Substring(BehaviorPrefix.Length);
            for (int i = 0; i < BehaviorKeys.Length; i++)
                if (string.Equals(key, BehaviorKeys[i], StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private static void Save()
        {
            if (_dir.Length == 0) return;
            try
            {
                string path = Path.Combine(_dir, FileName);
                Directory.CreateDirectory(_dir);
                bool allBehaviors = true;
                for (int i = 0; i < _behaviors.Length; i++)
                    if (!_behaviors[i]) { allBehaviors = false; break; }
                if (_map.Count == 0 && _pingPong && allBehaviors)
                {
                    if (File.Exists(path)) File.Delete(path);
                    return;
                }
                var keys = new List<string>(_map.Keys);
                keys.Sort(NaturalStringComparer.Compare);
                var lines = new List<string>
                {
                    "# Adomeji sprite roles — <file>=<role>; edit from the in-game Sprites tab",
                };
                if (!_pingPong) lines.Add(PingPongKey + "=false");
                for (int i = 0; i < _behaviors.Length; i++)
                    if (!_behaviors[i])
                        lines.Add(BehaviorPrefix + BehaviorKeys[i] + "=false");
                foreach (string key in keys) lines.Add(key + "=" + _map[key]);
                File.WriteAllLines(path, lines.ToArray());
            }
            catch (Exception e)
            {
                RuntimeContext.Log("[Sprites] roles: save failed (" + e.Message + ")");
            }
        }

        // Files explicitly assigned to a role, in animation order.
        public static List<string> AssignedTo(string role)
        {
            var list = new List<string>();
            foreach (var kv in _map)
                if (kv.Value == role) list.Add(kv.Key);
            list.Sort(NaturalStringComparer.Compare);
            return list;
        }

        // What the filename rules would use this file for, or null when
        // nothing matches (shown as "unused" in the editor).
        public static string AutoRoleOf(string baseName)
        {
            int end = baseName.Length;
            while (end > 0 && baseName[end - 1] >= '0' && baseName[end - 1] <= '9') end--;
            string stem = baseName.Substring(0, end).ToLowerInvariant();
            switch (stem)
            {
                case "death":
                case "miss": return "dead";
                case "crawl": return "climb";
                case "hang": return "ceiling";
                // flip*.png is a rude gesture, not a pose — off unless the
                // player picks a role for it in the Sprites tab
                case "flip": return Disabled;
                case "peek":
                case "ledge": return "edge";
                case "shime": return "shimeji";
                default: return IsValidRole(stem) && stem != Disabled ? stem : null;
            }
        }

        // "walk2" before "walk10": compare the non-numeric stem, then the
        // trailing number.
        public static int CompareNatural(string a, string b)
            => NaturalStringComparer.Compare(a, b);

        private static void SplitTrailingNumber(string s, out string stem, out int num)
        {
            int end = s.Length;
            while (end > 0 && s[end - 1] >= '0' && s[end - 1] <= '9') end--;
            stem = s.Substring(0, end);
            num = 0;
            if (end < s.Length) int.TryParse(s.Substring(end), out num);
        }
    }
}
