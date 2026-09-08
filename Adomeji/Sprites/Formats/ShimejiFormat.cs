using System.Collections.Generic;
using System.IO;
using Adomeji.Sprites.Loading;
using Adomeji.Composition;

namespace Adomeji.Sprites.Formats
{
    // Support for the standard Shimeji / Shimeji-ee image format, so a pack
    // downloaded from the internet drops into Sprites/ with no renaming.
    //
    // A shimeji image set is a folder of shime1.png .. shime46.png. The
    // numbering isn't arbitrary: the stock conf/actions.xml assigns each file
    // to an action, and packs made for Shimeji-ee follow it —
    //
    //   1,2,3      Stand / Walk cycle (1,2,1,3)      22       Jumping
    //   4          Falling                            23,24,25 Grab/ClimbCeiling
    //   5,6        Resisting (arm waving)             26-29    SitAndSpinHead
    //   7-10       Pinched (held by the mouse)        30-33    Sit variants / dangle legs
    //   11         Sit                                34-37    carry / throw the window
    //   12,13,14   Grab/ClimbWall                     38-41    PullUpShimeji
    //   15-17      SitAndSpinHead                     42-46    Divide
    //   18,19      Bouncing        20,21  Creep / Sprawl
    //
    // Layouts accepted (shime1.png is the marker, same file Shimeji-ee's image
    // set chooser looks for):
    //   Pack/shime1.png
    //   Pack/img/shime1.png
    //   Pack/img/<Character>/shime1.png
    //   Pack/<Character>/shime1.png
    //
    // A conf/actions.xml shipped with the pack wins over the numbering, so
    // packs that renumber or extend their art still land on the right poses.
    public sealed class ShimejiLayout
    {
        private const int MaxFrames = 16;

        // Folder the shime*.png files actually live in.
        public string ImageDir { get; private set; }

        // action name -> image basenames in animation order; null when the
        // pack ships no actions.xml and the stock numbering is used
        private readonly Dictionary<string, string[]> _actions;

        internal ShimejiLayout(string imageDir, Dictionary<string, string[]> actions)
        {
            ImageDir = imageDir;
            _actions = actions;
        }

        public bool HasActionsXml => _actions != null;

        // Frame basenames (no extension) for a pose, or null if this pack has
        // nothing for it — the caller then falls back as usual.
        public string[] Frames(Pose pose)
        {
            int p = (int)pose;
            if (p < 0 || p >= ShimejiFormat.Recipes.Length) return null;
            var recipe = ShimejiFormat.Recipes[p];

            if (_actions != null)
            {
                foreach (string action in recipe.Actions)
                {
                    string[] images;
                    if (!_actions.TryGetValue(action, out images)) continue;
                    string[] fromXml = Clean(images, recipe.Max);
                    if (fromXml.Length > 0) return fromXml;
                }
            }

            var byNumber = new string[recipe.Numbers.Length];
            for (int i = 0; i < recipe.Numbers.Length; i++)
                byNumber[i] = "shime" + recipe.Numbers[i];
            string[] stock = Clean(byNumber, recipe.Max);
            return stock.Length > 0 ? stock : null;
        }

        // Drop frames the pack doesn't actually ship, collapse repeats that
        // would stall the cycle (shimeji encodes hold time by repeating a
        // frame; we animate at a fixed rate), and cap the list — a stock
        // Resisting is 30 poses long, which is a loop, not a reaction.
        private string[] Clean(string[] names, int max)
        {
            var kept = new List<string>();
            if (max <= 0 || max > MaxFrames) max = MaxFrames;
            foreach (string name in names)
            {
                if (kept.Count >= max) break;
                if (!File.Exists(Path.Combine(ImageDir, name + ".png"))) continue;
                if (kept.Count > 0 && kept[kept.Count - 1] == name) continue;
                kept.Add(name);
            }
            return kept.ToArray();
        }
    }

    public static class ShimejiFormat
    {
        public const string Marker = "shime1.png";
        private static readonly string Conf = Path.Combine("conf", "actions.xml");

        internal struct Recipe
        {
            public string[] Actions; // preferred actions.xml names, best first
            public int[] Numbers;    // stock shime numbers, in animation order
            public int Max;          // frame cap; shimeji animations run long
        }

        // Pose -> how to build it out of a shimeji image set. Indexed by
        // (int)Pose, so this array tracks the enum.
        //
        // A few poses have no shimeji counterpart and borrow the nearest thing:
        // Happy takes Resisting (the arm-waving struggle reads as cheering),
        // Shock takes Pinched (dangling from the cursor), Bonk takes
        // Tripping. The rest map straight across.
        internal static readonly Recipe[] Recipes =
        {
            /* Walk    */ New(8, new[] { "Walk", "Stand" }, 1, 2, 1, 3),
            /* Sit     */ New(2, new[] { "Sit", "SitWithLegsDown", "SitAndLookUp" }, 11),
            /* Fall    */ New(2, new[] { "Jumping", "Falling" }, 22),
            /* Happy   */ New(4, new[] { "Resisting", "SitAndDangleLegs" }, 5, 6),
            /* Shock   */ New(1, new[] { "Pinched" }, 10),
            /* Dead    */ New(1, new[] { "Sprawl", "Creep" }, 21),
            /* Bonk    */ New(4, new[] { "Tripping", "Bouncing" }, 19, 18, 20),
            /* Falling */ New(2, new[] { "Falling", "Jumping" }, 4),
            /* Climb   */ New(6, new[] { "ClimbWall", "GrabWall" }, 14, 12, 13, 12),
            /* Ceiling */ New(6, new[] { "ClimbCeiling", "GrabCeiling" }, 25, 23, 24, 23),
            /* Jump    */ New(2, new[] { "Jumping", "Falling" }, 22),
        };

        private static Recipe New(int max, string[] actions, params int[] numbers) =>
            new Recipe { Actions = actions, Numbers = numbers, Max = max };

        // Is this folder itself a shimeji image set?
        public static bool IsImageSet(string dir) =>
            dir != null && dir.Length > 0 && File.Exists(Path.Combine(dir, Marker));

        // Resolve a pack folder into a layout, digging through the img/ and
        // img/<Character>/ wrappers real packs ship. Returns null when the
        // folder holds no shimeji art (a normal named pack, or nothing).
        public static ShimejiLayout Resolve(string packDir)
        {
            string imageDir = FindImageDir(packDir);
            if (imageDir == null) return null;
            return new ShimejiLayout(imageDir, LoadActions(packDir, imageDir));
        }

        public static string FindImageDir(string packDir)
        {
            if (packDir == null || packDir.Length == 0) return null;
            if (IsImageSet(packDir)) return packDir;
            var nested = ImageSetsUnder(packDir);
            return nested.Count > 0 ? nested[0] : null;
        }

        // Image sets nested below a pack folder, either directly or under img/
        // (a pack can bundle several characters that way). img/ is searched
        // first because that's where Shimeji-ee puts them.
        public static List<string> ImageSetsUnder(string packDir)
        {
            var found = new List<string>();
            foreach (string parent in new[] { Path.Combine(packDir, "img"), packDir })
            {
                if (!Directory.Exists(parent)) continue;
                if (IsImageSet(parent)) { Add(found, parent); continue; }
                foreach (string sub in Directory.GetDirectories(parent))
                    if (IsImageSet(sub)) Add(found, sub);
            }
            return found;
        }

        private static void Add(List<string> list, string dir)
        {
            if (!list.Contains(dir)) list.Add(dir);
        }

        // conf/actions.xml, wherever the pack chose to put it
        private static Dictionary<string, string[]> LoadActions(string packDir, string imageDir)
        {
            // imageDir is often Pack/img/<Character>, and the conf can sit next
            // to the images, beside img/, or at the pack root — check them all
            string[] candidates =
            {
                Path.Combine(imageDir, Conf),
                Path.Combine(imageDir, Path.Combine("..", Conf)),
                Path.Combine(imageDir, Path.Combine("..", Path.Combine("..", Conf))),
                Path.Combine(packDir, Conf),
                Path.Combine(packDir, "actions.xml"),
            };
            foreach (string path in candidates)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var parsed = ActionsXmlParser.Parse(File.ReadAllText(path));
                    if (parsed != null && parsed.Count > 0)
                    {
                        RuntimeContext.Log("[Sprites] shimeji: using " + path);
                        return parsed;
                    }
                }
                catch (System.Exception e)
                {
                    RuntimeContext.Log("[Sprites] shimeji: actions.xml unreadable (" + e.Message + ")");
                }
            }
            return null;
        }

    }
}
