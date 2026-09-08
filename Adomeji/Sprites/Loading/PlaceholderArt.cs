using System.Collections.Generic;
using System.IO;
using Adomeji.Sprites.Configuration;
using Adomeji.Sprites.Formats;
using Adomeji.Sprites.Selection;
using Adomeji.Composition;
using UnityEngine;

namespace Adomeji.Sprites.Loading
{
    // Pose groups; each carries 1..N animation frames.
    // Fall = gentle drift; Falling = fast/scared plummet (falls back to Fall art).
    // Climb/Ceiling/Jump are optional: packs that don't draw them borrow
    // Walk/Fall, which is what every pack did before shimeji-format support.
    // Idle/Wave are optional standing-around poses; a pack drawing neither
    // sits instead. Shock is the hard-miss reaction (Too Early / Too
    // Late / Multipress) and the first beat of a death before Dead art.
    public enum Pose { Walk, Sit, Fall, Happy, Shock, Dead, Bonk, Falling, Climb, Ceiling, Jump, Panic, Edge, Idle, Wave }

    // One loaded sprite pack. Each pose loads its frames from the pack's
    // folder: numbered files first (walk0.png, walk1.png, ... in order), then
    // a bare single (fall.png), then aliases (death.png/miss.png for dead —
    // death.png counts as pre-oriented art and is never rotated), then the
    // standard shime*.png numbering if the pack is in Shimeji format.
    // Packs can ship any frame count per pose — a 6-frame walk cycle just
    // works. Movement poses a pack doesn't draw borrow walk/fall; reaction
    // poses (happy/shock/dead/bonk) it doesn't draw simply disable that
    // reaction — no substitute art is ever shown.
    //
    // Several packs can be loaded at once (see PlaceholderArt.ApplyPacks);
    // each pet holds a reference to the pack it spawned as.
    public class PackArt
    {
        internal const int PoseCount = 15; // tracks the Pose enum
        private const int MaxFrames = 16;

        public string Pack { get; private set; }

        // Relative spawn share among the loaded packs (see RandomPack); set
        // from Settings.PackWeights by ApplyWeights.
        public float Weight = 1f;

        private readonly Sprite[][] Frames = new Sprite[PoseCount][];
        // Frames[p] may be borrowed from another pose; only the owner destroys it
        private readonly bool[] Owned = new bool[PoseCount];
        // pose came from the pack's own PNGs, not from the built-in placeholder
        private readonly bool[] FromDisk = new bool[PoseCount];
        // art is drawn already oriented for the pose (e.g. death.png is drawn
        // lying down), so the mod must not add its own rotation on top
        private readonly bool[] PreOriented = new bool[PoseCount];
        private readonly bool[] Behaviors = new bool[SpriteConfig.BehaviorKeys.Length];

        // folder the PNGs are actually read from (shimeji packs nest theirs)
        private string _imageDir = "";
        private ShimejiLayout _shimeji;

        // Shimeji-format art is drawn already oriented for climbing, hanging
        // and lying down — the mod must not rotate it on top of that.
        public bool IsShimejiPack => _shimeji != null;

        // folder this pack's PNGs live in (the sprite editor lists it)
        public string ImageDir => _imageDir;

        // walk cycles 1..N..1 instead of looping — a property of how the pack
        // draws its walk frames, so it lives in the pack's role file
        public bool PingPongWalk { get; private set; } = true;

        public bool Allows(PackBehavior behavior) => Behaviors[(int)behavior];

        public Sprite Get(Pose pose, int frame)
        {
            var arr = Frames[(int)pose];
            if (arr == null || arr.Length == 0) return null;
            return arr[frame > 0 ? frame % arr.Length : 0];
        }

        public int FrameCount(Pose pose)
        {
            var arr = Frames[(int)pose];
            return arr == null || arr.Length == 0 ? 1 : arr.Length;
        }

        // Does this pose have anything to draw at all (own or borrowed art)?
        // Reactions with no art are skipped entirely by the state machine.
        public bool HasArt(Pose pose)
        {
            var arr = Frames[(int)pose];
            return arr != null && arr.Length > 0;
        }

        // Reaction poses this pack doesn't draw, with the feature each one
        // disables — surfaced as a warning in the Sprites settings tab.
        public List<string> MissingReactions()
        {
            var list = new List<string>();
            if (Allows(PackBehavior.Celebrate) && !HasArt(Pose.Happy))
                list.Add("happy.png — no cheering on level clears");
            if (Allows(PackBehavior.Judgement) && !HasArt(Pose.Shock))
                list.Add("shock.png — no reaction on Too Early / Too Late");
            if (Allows(PackBehavior.Death) && !HasArt(Pose.Dead))
                list.Add("dead.png (or death.png / miss.png) — no miss/death reactions");
            if (Allows(PackBehavior.Bonk) && !HasArt(Pose.Bonk))
                list.Add("bonk.png — no bonk on crashes");
            return list;
        }

        // True when the pack ships art already drawn in the right orientation
        // for this pose — climbing, hanging, sprawled. Adding the mod's own
        // rotation on top of shimeji art would double it up.
        public bool Oriented(Pose pose) =>
            PreOriented[(int)pose] || (IsShimejiPack && FromDisk[(int)pose]);

        // Which way a pose's art is drawn: +1 right, -1 left. Named packs
        // face right; shimeji art faces left by convention (shime*.png are
        // drawn left-facing and the reference engine mirrors them for the
        // other direction), so mirroring it on the same rule walks the
        // character backwards. Per-pose, because a shimeji pack can borrow a
        // right-facing pose for a file it's missing.
        public float Facing(Pose pose) =>
            IsShimejiPack && FromDisk[(int)pose] ? -1f : 1f;

        // Load a pack's art from disk; clears and rebuilds every pose.
        // Movement poses missing from the pack borrow walk/fall; missing
        // reaction poses stay empty and that reaction is skipped.
        public void Load(string pack)
        {
            Pack = pack ?? PlaceholderArt.DefaultPack;
            ClearFrames();

            _imageDir = PlaceholderArt.PackDir(Pack);
            _shimeji = _imageDir.Length == 0 ? null : ShimejiFormat.Resolve(_imageDir);

            // configured pack is gone or holds nothing (deleted folder, or an
            // old save pointing at the removed built-in art): fall back to
            // the bundled ELLIE pack rather than drawing nothing
            if (_shimeji == null && !PlaceholderArt.DirHasArt(_imageDir))
            {
                string fallback = PlaceholderArt.PackDir(PlaceholderArt.DefaultPack);
                if (_imageDir != fallback)
                {
                    RuntimeContext.Log("[Sprites] pack '" + PlaceholderArt.DisplayName(Pack)
                        + "' has no art — using bundled " + PlaceholderArt.DefaultPack);
                    _imageDir = fallback;
                    _shimeji = ShimejiFormat.Resolve(_imageDir);
                }
            }

            if (_shimeji != null)
            {
                _imageDir = _shimeji.ImageDir; // pack nested its art in img/
                RuntimeContext.Log("[Sprites] applying pack: " + PlaceholderArt.DisplayName(Pack)
                    + " (Shimeji format, "
                    + (_shimeji.HasActionsXml ? "actions.xml" : "stock numbering") + ")");
            }
            else
            {
                RuntimeContext.Log("[Sprites] applying pack: " + PlaceholderArt.DisplayName(Pack));
            }

            // per-file role overrides live next to the art (SpriteConfig is a
            // single static map — packs load sequentially, each pointing it at
            // its own folder first)
            SpriteConfig.Load(_imageDir);
            PingPongWalk = SpriteConfig.PingPong;
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i] = SpriteConfig.BehaviorEnabled((PackBehavior)i);

            LoadPose(Pose.Walk, "walk", null);
            LoadOrBorrow(Pose.Sit, "sit", Pose.Walk);
            LoadOrBorrow(Pose.Fall, "fall", Pose.Walk);
            // reactions: no borrow — a pack without the art skips the reaction
            LoadPose(Pose.Happy, "happy", null);
            LoadPose(Pose.Shock, "shock", null);
            LoadPose(Pose.Dead, "dead", "death", "miss");
            // dead art (dead/death/miss.png) is drawn exactly as it should
            // display — never tip it 90° on top
            PreOriented[(int)Pose.Dead] = true;
            LoadPose(Pose.Bonk, "bonk", null);
            // poses only some packs draw: borrow an existing pose
            LoadOrBorrow(Pose.Falling, "falling", Pose.Fall, "fall");
            // climb.png / crawl.png = wall-climb art drawn upright as it should
            // display: never tipped 90° onto the wall, only mirrored to face
            // the side it's climbing. Packs with no climb art borrow walk,
            // which does get rotated.
            LoadOrBorrow(Pose.Climb, "climb", Pose.Walk, "crawl");
            PreOriented[(int)Pose.Climb] = FromDisk[(int)Pose.Climb];
            // ceiling.png / hang.png = ceiling art drawn upside-down already,
            // as it should display: never spun another 180° on top. Packs with
            // no ceiling art borrow walk, which does get rotated.
            LoadOrBorrow(Pose.Ceiling, "ceiling", Pose.Walk, "hang");
            PreOriented[(int)Pose.Ceiling] = FromDisk[(int)Pose.Ceiling];
            // no "flip" alias: flip*.png is a rude gesture, not a jump —
            // it stays unused until a pack assigns it a role by hand
            LoadOrBorrow(Pose.Jump, "jump", Pose.Fall);
            // panic: tile about to scroll off screen / planet incoming; a pack
            // without the art just never panics
            LoadPose(Pose.Panic, "panic", null);
            // edge: peering down over a ledge (screen corner / tile lip); a
            // pack without the art just walks up and turns around
            LoadPose(Pose.Edge, "edge", "peek", "ledge");
            // standing around between strolls; a pack with neither sits
            LoadPose(Pose.Idle, "idle", null);
            LoadPose(Pose.Wave, "wave", null);

            PackAtlas();

            if (!HasArt(Pose.Walk))
                RuntimeContext.Log("[Sprites] pack has no walk art — pets will be invisible");
        }

        public void Unload() => ClearFrames();

        // Every frame loads as its own texture, and uGUI can only batch draws
        // that share one. A hundred pets on a hundred textures is a hundred
        // draw calls; repacked onto a single atlas they collapse into one
        // (one atlas per loaded pack).
        private Texture2D _atlas;

        private void PackAtlas()
        {
            var texs = new List<Texture2D>();
            long area = 0;
            for (int p = 0; p < PoseCount; p++)
            {
                if (!Owned[p] || Frames[p] == null) continue;
                foreach (var s in Frames[p])
                {
                    if (s == null || s.texture == null) continue;
                    texs.Add(s.texture);
                    area += s.texture.width * s.texture.height;
                }
            }
            // nothing to batch, or the pack is big enough that PackTextures
            // would silently downscale it — leave those alone, one draw call
            // is not worth blurring someone's art
            if (texs.Count < 2 || area > 4096L * 4096L / 2L) { FreeCpuCopies(texs); return; }

            int maxW = 0;
            for (int i = 0; i < texs.Count; i++)
                if (texs[i].width > maxW) maxW = texs[i].width;

            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Rect[] uv;
            try { uv = atlas.PackTextures(texs.ToArray(), 2, 4096, false); }
            catch { uv = null; }
            if (uv == null || uv.Length != texs.Count)
            {
                Object.Destroy(atlas);
                FreeCpuCopies(texs);
                return;
            }

            // same rule as the individual loads: pixel art stays crisp
            atlas.filterMode = maxW >= 64 ? FilterMode.Bilinear : FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.Apply(false, true); // GPU-only from here

            int k = 0;
            for (int p = 0; p < PoseCount; p++)
            {
                if (!Owned[p] || Frames[p] == null) continue;
                var arr = Frames[p];
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == null || arr[i].texture == null) continue;
                    Rect r = uv[k++];
                    var old = arr[i];
                    arr[i] = Sprite.Create(atlas,
                        new Rect(r.x * atlas.width, r.y * atlas.height,
                            r.width * atlas.width, r.height * atlas.height),
                        new Vector2(0.5f, 0.5f), r.width * atlas.width, 0,
                        SpriteMeshType.FullRect, Vector4.zero, false);
                    Object.Destroy(old.texture);
                    Object.Destroy(old);
                }
            }
            _atlas = atlas;
        }

        // Pack frames load readable (PackTextures needs to read them); when
        // packing doesn't happen, drop the CPU-side pixel copies anyway.
        private static void FreeCpuCopies(List<Texture2D> texs)
        {
            for (int i = 0; i < texs.Count; i++)
                if (texs[i] != null) texs[i].Apply(false, true);
        }

        private void ClearFrames()
        {
            // packed frames all share the atlas; destroying it once is enough
            // (the per-sprite Destroy below then no-ops on it)
            if (_atlas != null) { Object.Destroy(_atlas); _atlas = null; }
            for (int p = 0; p < PoseCount; p++)
            {
                var arr = Frames[p];
                Frames[p] = null;
                FromDisk[p] = false;
                PreOriented[p] = false;
                if (arr == null || !Owned[p]) { Owned[p] = false; continue; }
                Owned[p] = false;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == null) continue;
                    Object.Destroy(arr[i].texture);
                    Object.Destroy(arr[i]);
                }
            }
        }

        private void LoadPose(Pose pose, string name, params string[] aliases)
        {
            var list = LoadFromPack(name, aliases, pose);
            if (list.Count == 0) return; // ClearFrames left it empty: pose disabled

            Frames[(int)pose] = list.ToArray();
            Owned[(int)pose] = true;
            FromDisk[(int)pose] = true;
        }

        private void LoadOrBorrow(Pose pose, string name, Pose borrow,
            params string[] aliases)
        {
            var list = LoadFromPack(name, aliases, pose);
            if (list.Count > 0)
            {
                Frames[(int)pose] = list.ToArray();
                Owned[(int)pose] = true;
                FromDisk[(int)pose] = true;
                return;
            }
            Frames[(int)pose] = Frames[(int)borrow];
            Owned[(int)pose] = false;
            FromDisk[(int)pose] = false;
        }

        // Named files first (walk0..N, then walk, then each alias in order), so
        // dropping a hand-drawn happy0.png into a shimeji pack overrides the
        // mapping — all skipping files the user disabled or reassigned in the
        // sprite editor. Files explicitly assigned to this pose then join (or
        // form) the list.
        private List<Sprite> LoadFromPack(string name, string[] aliases, Pose pose)
        {
            var names = new List<string>();

            SeriesNames(names, name);
            if (names.Count == 0 && aliases != null)
                foreach (string alias in aliases)
                {
                    SeriesNames(names, alias);
                    if (names.Count > 0) break;
                }
            if (names.Count == 0 && _shimeji != null)
            {
                string[] frames = _shimeji.Frames(pose);
                if (frames != null)
                    foreach (string frame in frames)
                        if (!SpriteConfig.HasExplicit(frame)) names.Add(frame);
            }

            // user-assigned files: extra frames for the pose, or the whole
            // pose when nothing auto-loaded (AssignedTo is already in order)
            var assigned = SpriteConfig.AssignedTo(SpriteConfig.RoleName(pose));
            foreach (string file in assigned)
                if (!names.Contains(file)) names.Add(file);

            var list = new List<Sprite>(names.Count);
            foreach (string n in names)
            {
                var s = PlaceholderArt.LoadFromDisk(_imageDir, n);
                if (s != null) list.Add(s);
            }
            return list;
        }

        private void SeriesNames(List<string> names, string name)
        {
            if (_imageDir.Length == 0) return;
            for (int i = 0; i < MaxFrames; i++)
            {
                string n = name + i;
                if (!File.Exists(Path.Combine(_imageDir, n + ".png"))) break;
                if (SpriteConfig.HasExplicit(n)) continue; // disabled or reassigned
                names.Add(n);
            }
            if (names.Count == 0
                && File.Exists(Path.Combine(_imageDir, name + ".png"))
                && !SpriteConfig.HasExplicit(name))
            {
                names.Add(name);
            }
        }
    }

    // Pack discovery + the registry of currently loaded packs. Pets spawn as
    // a random loaded pack (see Shimeji.Init / RerollArt).
    public static class PlaceholderArt
    {
        // pack ids: "/" = loose PNGs in Sprites/, anything else = a folder
        // under Sprites/ (may be nested, "Pack/Char")
        public const string RootPack = "/";
        // bundled pack, shipped with the mod; the fallback when the
        // configured pack is missing or holds no art
        public const string DefaultPack = "ELLIE";

        private static readonly List<PackArt> _loaded = new List<PackArt>();
        public static IList<PackArt> LoadedPacks => _loaded;

        public static void UnloadAll()
        {
            foreach (var art in _loaded) art.Unload();
            _loaded.Clear();
        }

        // (Re)load the given selection. Always reloads from disk — called at
        // click frequency (pack toggles, role edits), not per frame.
        public static void ApplyPacks(List<string> packs)
        {
            UnloadAll();

            var seen = new HashSet<string>();
            if (packs != null)
                foreach (string id in packs)
                {
                    if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;
                    var art = new PackArt();
                    art.Load(id);
                    _loaded.Add(art);
                }

            // empty selection: fall back to the bundled pack rather than
            // spawning invisible pets
            if (_loaded.Count == 0)
            {
                var art = new PackArt();
                art.Load(DefaultPack);
                _loaded.Add(art);
            }

            ApplyWeights(packs, RuntimeContext.Settings.PackWeights);
        }

        // Push the mix-slider shares onto the loaded packs. Separate from
        // ApplyPacks because dragging the slider must not reload every texture.
        public static void ApplyWeights(List<string> packs, List<float> weights)
        {
            if (packs == null || weights == null) return;
            for (int i = 0; i < packs.Count && i < weights.Count; i++)
            {
                var art = PackFor(packs[i]);
                if (art != null) art.Weight = weights[i];
            }
        }

        // A random loaded pack — each pet rolls one at spawn, weighted by the
        // packs' spawn shares, so two equally weighted packs give a 50/50
        // flock and a 70/30 mix spawns roughly seven pets in ten as the first.
        public static PackArt RandomPack()
        {
            int n = _loaded.Count;
            if (n == 0) return null;
            int index = WeightedIndexSelector.Select(
                n,
                i => _loaded[i].Weight,
                Random.value,
                Random.Range(0, n));
            return _loaded[index];
        }

        public static PackArt PackFor(string id)
        {
            foreach (var art in _loaded)
                if (art.Pack == id) return art;
            return null;
        }

        // one-off sprite load for the settings UI's thumbnails; the caller
        // owns the returned sprite + texture and must destroy them
        public static Sprite LoadThumb(string dir, string baseName) =>
            LoadFromDisk(dir, baseName);

        // 64-entry rainbow lookup table: HSVToRGB is beneath a Pentium's dignity
        private static readonly Color[] Rainbow = BuildRainbow();

        public static Color RainbowAt(float t)
        {
            int i = (int)(Mathf.Repeat(t, 1f) * 64f);
            return Rainbow[i < 64 ? i : 63];
        }

        private static Color[] BuildRainbow()
        {
            var table = new Color[64];
            for (int i = 0; i < 64; i++)
                table[i] = Color.HSVToRGB(i / 64f, 0.55f, 1f);
            return table;
        }

        public static string SpritesRoot() => RuntimeContext.SpritesPath;

        // Absolute folder for a pack id, before shimeji nesting is resolved.
        internal static string PackDir(string pack)
        {
            if (string.IsNullOrEmpty(pack)) return "";
            string userRoot = SpritesRoot();
            if (pack == RootPack) return userRoot;
            string user = Path.Combine(userRoot, pack);
            if (DirHasArt(user) || ShimejiFormat.ImageSetsUnder(user).Count > 0) return user;
            return Path.Combine(RuntimeContext.BundledSpritesPath, pack);
        }

        // Does this folder hold art we can load directly?
        internal static bool DirHasArt(string dir) =>
            File.Exists(Path.Combine(dir, "walk0.png"))
            || File.Exists(Path.Combine(dir, "walk.png"))
            || ShimejiFormat.IsImageSet(dir);

        // Discover available packs: loose PNGs in Sprites/, and each
        // Sprites/<subfolder>/. A folder that only wraps image sets (a shimeji
        // pack bundling several characters, or the usual img/<Name>/ layout)
        // is listed once per character instead of once for the wrapper.
        public static string[] ScanPacks()
        {
            var packs = new List<string>();
            try
            {
                ScanRoot(SpritesRoot(), packs, true);
                ScanRoot(RuntimeContext.BundledSpritesPath, packs, false);
                RuntimeContext.Log($"[Sprites] scanned user and bundled roots: " +
                    string.Join(", ", packs.ConvertAll(DisplayName).ToArray()));
            }
            catch (System.Exception e) { RuntimeContext.Log("[Sprites] scan failed: " + e.Message); }
            return packs.ToArray();
        }

        private static void ScanRoot(string root, List<string> packs, bool includeLoose)
        {
            if (!Directory.Exists(root)) return;
            if (includeLoose && DirHasArt(root) && !packs.Contains(RootPack)) packs.Add(RootPack);
            foreach (var dir in Directory.GetDirectories(root))
            {
                string name = Path.GetFileName(dir);
                if (DirHasArt(dir)) { if (!packs.Contains(name)) packs.Add(name); continue; }
                var sets = ShimejiFormat.ImageSetsUnder(dir);
                if (sets.Count < 2) { if (!packs.Contains(name)) packs.Add(name); continue; }
                foreach (string set in sets)
                {
                    string id = name + "/" + Relative(dir, set);
                    if (!packs.Contains(id)) packs.Add(id);
                }
            }
        }

        // "<root>/Pack/img/Mee" under "<root>/Pack" -> "img/Mee"
        private static string Relative(string parent, string child)
        {
            string tail = child.Length > parent.Length ? child.Substring(parent.Length) : child;
            return tail.Replace('\\', '/').Trim('/');
        }

        public static string DisplayName(string pack)
        {
            if (string.IsNullOrEmpty(pack)) return DefaultPack;
            if (pack == RootPack) return "Sprites/ (loose files)";
            // "Bundle/img/Mee" reads as "Bundle / Mee"; img/ is plumbing
            return pack.Replace("/img/", "/").Replace("/", " / ");
        }

        internal static Sprite LoadFromDisk(string dir, string name)
        {
            if (string.IsNullOrEmpty(dir)) return null;
            try
            {
                string path = Path.Combine(dir, name + ".png");
                if (!File.Exists(path)) return null;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                // stays readable: PackAtlas has to read these back to build the
                // shared atlas, and drops the CPU-side copies once it's done
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), false)) return null;
                // pixel art stays crisp; larger art gets smoothed
                tex.filterMode = tex.width >= 64 ? FilterMode.Bilinear : FilterMode.Point;
                return MakeSprite(tex);
            }
            catch
            {
                return null;
            }
        }

        // The UI Image draws a simple quad regardless of sprite mesh, so the
        // default Sprite.Create (Tight mesh + fallback physics shape — both of
        // which trace the alpha outline of every frame at load) is pure waste.
        // FullRect + no physics shape skips all of it and works on
        // non-readable textures.
        private static Sprite MakeSprite(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
    }
}
