using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Adomeji.Composition;
using Adomeji.Settings;
using Adomeji.Sprites.Catalog;
using Adomeji.Sprites.Configuration;
using Adomeji.Sprites.Loading;
using Adomeji.UI.Framework;
using Adomeji.UI.Services;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Pose = Adomeji.Sprites.Loading.Pose;

namespace Adomeji.UI.Tabs;

internal sealed class SpritesTabPresenter : ISettingsTabPresenter
{
  private readonly ISpritePackCatalog _catalog;
  private readonly ISpritePackLoader _loader;
  private readonly IPetCustomizationService _pets;
  private readonly ISpriteRoleStore _roles;
  private readonly UiResourceScope _resources = new UiResourceScope();
  private AdomejiSettings _settings;
  private UiElementFactory _ui;
  private DynamicBindingRegistry _bindings;
  private UiTheme _theme;
  private RectTransform _packListRoot;
  private string _editingPack;
  private string _rolePickFile;
  private Text _packWarning;

  public string Title => "Sprites";

  public SpritesTabPresenter(ISpritePackCatalog catalog, ISpritePackLoader loader,
    IPetCustomizationService pets, ISpriteRoleStore roles)
  {
    _catalog = catalog;
    _loader = loader;
    _pets = pets;
    _roles = roles;
  }

  public void Build(RectTransform page, UiBuildContext context)
  {
      _settings = context.Settings;
      _ui = context.Elements;
      _bindings = context.Bindings;
      _theme = context.Theme;

      _ui.AddSlider(page, "Number of pets", 1, () => _settings.MaxPets, true, null,
          () => _settings.Count,
          v => _settings.Count = Mathf.RoundToInt(v));
      _ui.AddToggle(page, "Allow up to 100 pets (heavy CPU load)",
          () => _settings.RaisePetLimit,
          v => _settings.RaisePetLimit = v);
      _bindings.Add(() =>
      {
          if (!_settings.RaisePetLimit && _settings.Count > 8)
              _settings.Count = 8;
      });

      _ui.AddSlider(page, "Pet size", 0.5f, () => _settings.MaxScale, false, "x",
          () => _settings.Scale,
          v => _settings.Scale = v);
      _ui.AddToggle(page, "Allow pet size up to 20x",
          () => _settings.UncapScale,
          v => _settings.UncapScale = v);
      _bindings.Add(() =>
      {
          if (!_settings.UncapScale && _settings.Scale > 3f)
              _settings.Scale = 3f;
      });

      _ui.AddSlider(page, "Movement speed", 0.5f, () => 2.5f, false, "x",
          () => _settings.Speed,
          v => _settings.Speed = v);

      _ui.AddToggle(page, "Pets shrink and grow with the camera zoom",
          () => _settings.ZoomScale,
          v => _settings.ZoomScale = v);
      _ui.AddToggle(page, "Screen filters affect pets (level filter effects — grayscale, VHS, blur... — apply to pets too)",
          () => _settings.FilterPets,
          v => _settings.FilterPets = v);
      _ui.AddToggle(page, "Performance mode (pets update 30 times a second — lighter on the CPU)",
          () => _settings.OptimizedMode,
          v => _settings.OptimizedMode = v);

      var rescan = _ui.MakeButton(page, "Rescan", "Rescan Sprites folder", 17,
          () => RebuildPackList());
      rescan.gameObject.AddComponent<LayoutElement>().minHeight = 36;

      var openFolder = _ui.MakeButton(page, "OpenSprites", "Open Sprites folder", 17,
          () => OpenSpritesFolder());
      openFolder.gameObject.AddComponent<LayoutElement>().minHeight = 36;

      var listGo = _ui.NewRect("PackList", page);
      var listLayout = listGo.gameObject.AddComponent<VerticalLayoutGroup>();
      listLayout.spacing = 4;
      listLayout.childControlWidth = true;
      listLayout.childControlHeight = true;
      listLayout.childForceExpandWidth = true;
      listLayout.childForceExpandHeight = false;
      listGo.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
          ContentSizeFitter.FitMode.PreferredSize;
      _packListRoot = listGo;

      _packWarning = _ui.MakeText(page, "PackWarning", "", 15,
          new Color(1f, 0.72f, 0.35f), TextAnchor.UpperLeft).GetComponent<Text>();
      _packWarning.horizontalOverflow = HorizontalWrapMode.Wrap;
      _packWarning.verticalOverflow = VerticalWrapMode.Overflow;
      _packWarning.gameObject.SetActive(false);
  }

  // Reveal Sprites/ in the OS file browser so packs can be dropped in
  // without hunting for the mod folder. Created first — a fresh install
  // that never scanned has no folder to open.
  private void OpenSpritesFolder()
  {
      string root = _loader.RootDirectory;
      if (string.IsNullOrEmpty(root)) return;
      try
      {
          System.IO.Directory.CreateDirectory(root);
          Application.OpenURL(new Uri(root).AbsoluteUri); // escapes spaces
      }
      catch (Exception e) { RuntimeContext.Log("[Sprites] open folder failed: " + e.Message); }
  }

  private void RebuildPackList()
  {
      if (_packListRoot == null) return;
      for (int i = _packListRoot.childCount - 1; i >= 0; i--)
          Object.Destroy(_packListRoot.GetChild(i).gameObject);
      _resources.Clear();

      // the editor edits a SELECTED pack's files; if it dropped out of
      // the selection (deselected, deleted), fall back to the pack list
      if (_editingPack != null && _catalog.Find(_editingPack) == null)
      {
          _editingPack = null;
          _rolePickFile = null;
      }

      if (_editingPack != null) BuildSpriteEditor();
      else BuildPackRows();

      RefreshPackWarning();
  }

  // Multi-select: clicking a pack toggles it. Every selected pack is
  // loaded at once and each pet spawns as a random one — two packs
  // selected = a 50/50 flock. The last selected pack can't be unchecked.
  private void BuildPackRows()
  {
      string[] packs = _catalog.Scan();
      var sel = _settings.SpritePacks;
      _settings.SyncPackWeights();
      var weights = _settings.PackWeights;
      foreach (string pack in packs)
      {
          string captured = pack;
          bool isCurrent = sel.Contains(pack);

          var row = _ui.AddRow(_packListRoot, 34);
          var btn = _ui.MakeButton(row, "Pack",
              (isCurrent ? "✓  " : "") + _catalog.DisplayName(pack), 17, () =>
          {
              int at = sel.IndexOf(captured);
              if (at >= 0)
              {
                  if (sel.Count > 1) // keep at least one
                  {
                      sel.RemoveAt(at);
                      // weights are parallel: drop the same index, not the tail
                      if (at < weights.Count) weights.RemoveAt(at);
                  }
              }
              else
              {
                  sel.Add(captured);
                  weights.Add(AverageWeight(weights)); // joins the mix evenly
              }
              // apply immediately so the missing-sprite warning below
              // describes the packs that are actually selected
              _pets.ApplySelectedPacks();
              RebuildPackList();
          });
          var le = btn.gameObject.AddComponent<LayoutElement>();
          le.flexibleWidth = 1;
          le.minHeight = 34;
          btn.GetComponent<Image>().color = isCurrent ? _theme.AccentDim : _theme.PanelBackground;
          var txt = btn.GetComponentInChildren<Text>();
          txt.alignment = TextAnchor.MiddleLeft;
          txt.color = isCurrent ? _theme.Text : _theme.DimText;

          // per-file role editor (selects + applies the pack first, so
          // the editor always describes what's actually on screen)
          var edit = _ui.MakeButton(row, "Edit", "Edit ▸", 15, () =>
          {
              if (!sel.Contains(captured))
              {
                  sel.Add(captured);
                  weights.Add(AverageWeight(weights));
              }
              _pets.ApplySelectedPacks();
              _editingPack = captured;
              _rolePickFile = null;
              RebuildPackList();
          });
          var editLe = edit.gameObject.AddComponent<LayoutElement>();
          editLe.preferredWidth = 84;
          editLe.minHeight = 34;
          edit.GetComponentInChildren<Text>().color = _theme.DimText;
      }

      if (sel.Count > 1) BuildMixSlider(sel, weights);
  }

  // ---- spawn mix slider ----------------------------------------------

  private static float AverageWeight(List<float> weights)
  {
      if (weights.Count == 0) return 1f;
      float sum = 0f;
      foreach (float w in weights) sum += w;
      return sum / weights.Count;
  }

  // One bar split into a colored band per selected pack, each showing
  // that pack's sprite, with a draggable handle on every boundary: drag
  // it and the two packs either side trade spawn share. Shown once two
  // or more packs are selected (with one there is nothing to divide).
  //
  // The bands are plain Images anchored to the running totals, so they
  // resize themselves; the N-1 handles are full-width Sliders with no
  // fill, layered on top, which buys Unity's own drag handling without
  // any custom pointer code.
  private void BuildMixSlider(List<string> sel, List<float> weights)
  {
      const float BandHeight = 44f; // sprite on top, share text below
      const float PicSize = 26f;

      int n = sel.Count;

      // work in shares of 1 so a boundary is just the running total
      float total = 0f;
      for (int i = 0; i < n; i++) total += weights[i];
      for (int i = 0; i < n; i++)
          weights[i] = total > 0f ? weights[i] / total : 1f / n;

      float Cum(int i)
      {
          if (i < 0) return 0f;
          float sum = 0f;
          for (int k = 0; k <= i && k < n; k++) sum += weights[k];
          return i >= n - 1 ? 1f : sum; // the last boundary is pinned
      }

      var colors = new Color[n];
      for (int i = 0; i < n; i++) colors[i] = _theme.PackColor(i, n);

      var bar = _ui.NewRect("MixBar", _packListRoot);
      var barLe = bar.gameObject.AddComponent<LayoutElement>();
      barLe.minHeight = BandHeight + 8;
      barLe.preferredHeight = BandHeight + 8;

      var bandArea = _ui.NewRect("Bands", bar);
      bandArea.anchorMin = new Vector2(0, 0.5f);
      bandArea.anchorMax = new Vector2(1, 0.5f);
      bandArea.sizeDelta = new Vector2(0, BandHeight);

      // one band per pack, anchored to its slice of the bar — the drag
      // just rewrites the anchors and the layout does the resizing
      var bands = new RectTransform[n];
      var bandTexts = new Text[n];
      for (int i = 0; i < n; i++)
      {
          var band = _ui.NewRect("Band" + i, bandArea);
          band.anchorMin = new Vector2(0, 0);
          band.anchorMax = new Vector2(0, 1);
          band.offsetMin = Vector2.zero;
          band.offsetMax = Vector2.zero;
          var bandImg = band.gameObject.AddComponent<Image>();
          bandImg.color = colors[i];
          bandImg.raycastTarget = false;
          // a narrow band clips its contents rather than shrinking them away
          band.gameObject.AddComponent<RectMask2D>();
          bands[i] = band;

          // the pack's own art, so it's obvious which slice is whose.
          // These sprites belong to the loaded pack (no copy, nothing to
          // destroy) — every path that reloads packs rebuilds this list.
          // Sits in the upper half; the share text takes the lower.
          var pic = _ui.NewRect("Pic", band);
          pic.anchorMin = pic.anchorMax = new Vector2(0.5f, 0.5f);
          pic.sizeDelta = new Vector2(PicSize, PicSize);
          pic.anchoredPosition = new Vector2(0, 6);
          var picImg = pic.gameObject.AddComponent<Image>();
          var art = _catalog.Find(sel[i]);
          var thumb = art != null ? art.Get(Pose.Walk, 0) : null;
          if (thumb != null)
          {
              picImg.sprite = thumb;
              picImg.preserveAspect = true;
          }
          else picImg.color = new Color(1, 1, 1, 0f);
          picImg.raycastTarget = false;

          // "62% / 5" along the band's bottom edge; bands are bright
          // rainbow tints, so black stays readable on all of them
          var share = _ui.MakeText(band, "Share", "", 13,
              new Color(0, 0, 0, 0.8f), TextAnchor.LowerCenter);
          _ui.Stretch(share, 0, 0, 1, 1);
          share.offsetMin = new Vector2(0, 1);
          var shareText = share.GetComponent<Text>();
          shareText.horizontalOverflow = HorizontalWrapMode.Overflow; // mask clips
          shareText.raycastTarget = false;
          bandTexts[i] = shareText;
      }

      void LayoutBands()
      {
          for (int i = 0; i < n; i++)
          {
              bands[i].anchorMin = new Vector2(Cum(i - 1), 0);
              bands[i].anchorMax = new Vector2(Cum(i), 1);
              bands[i].offsetMin = Vector2.zero;
              bands[i].offsetMax = Vector2.zero;
          }
      }
      LayoutBands();

      // Share text lives inside each band; a band too narrow to fit its
      // text gets it in the fallback line under the bar instead.
      Text mixLabel = null;
      // per-pack counts the flock currently on screen was rolled with:
      // a drag that moves any of them leaves the bar describing a mix
      // nobody out there has, so the Respawn button lights up until
      // it's pressed
      var counts = new int[n];
      int[] shownCounts = null;
      Image respawnImg = null;
      Text respawnText = null;
      void UpdateLabel()
      {
          int pets = Mathf.Clamp(_settings.Count, 1, _settings.MaxPets);
          // 0 on the very first frame (layout hasn't run); everything
          // falls below for one frame and the first drag corrects it
          float barW = bandArea.rect.width;

          var below = new System.Text.StringBuilder();
          float cum = 0f;
          int taken = 0; // rounding the running total instead of each
                         // share keeps the counts summing to the pet count
          for (int i = 0; i < n; i++)
          {
              cum += weights[i];
              int upTo = i == n - 1 ? pets : Mathf.RoundToInt(cum * pets);
              int mine = upTo - taken;
              taken = upTo;
              counts[i] = mine;

              string s = Mathf.RoundToInt(weights[i] * 100f) + "% / " + mine;
              // ~8px per character at this size, plus breathing room
              bool fits = weights[i] * barW >= s.Length * 8f + 12f;
              if (bandTexts[i] != null) bandTexts[i].text = fits ? s : "";
              if (!fits)
              {
                  if (below.Length > 0) below.Append("   ");
                  below.Append("<color=#")
                       .Append(ColorUtility.ToHtmlStringRGB(colors[i])).Append('>')
                       .Append(_catalog.DisplayName(sel[i])).Append(' ')
                       .Append(s).Append("</color>");
              }
          }
          if (mixLabel != null) mixLabel.text = below.ToString();

          if (shownCounts == null) shownCounts = (int[])counts.Clone();
          bool stale = false;
          for (int i = 0; i < n; i++)
              if (counts[i] != shownCounts[i]) { stale = true; break; }
          if (respawnImg != null) respawnImg.color = stale ? _theme.Accent : _theme.PanelBackground;
          if (respawnText != null)
              respawnText.text = stale ? "Respawn pets to apply" : "Respawn pets";
      }

      const float MinShare = 0.02f; // a pack never drops out of the mix
      for (int i = 0; i < n - 1; i++)
      {
          int idx = i;
          var sliderGo = _ui.NewRect("Split" + i, bar);
          _ui.Stretch(sliderGo, 0, 0, 1, 1);
          var slider = sliderGo.gameObject.AddComponent<Slider>();

          // inset by the handle width so the grip stays inside the bar
          var handleArea = _ui.NewRect("Handle Area", sliderGo);
          handleArea.anchorMin = new Vector2(0, 0.5f);
          handleArea.anchorMax = new Vector2(1, 0.5f);
          handleArea.sizeDelta = new Vector2(-22, 0);
          var handle = _ui.NewRect("Handle", handleArea);
          handle.sizeDelta = new Vector2(22, 22);
          var handleImg = handle.gameObject.AddComponent<Image>();
          handleImg.sprite = KnobSprite();
          handleImg.color = _theme.Text; // the bands carry the colors now

          slider.handleRect = handle;
          slider.targetGraphic = handleImg;
          slider.direction = Slider.Direction.LeftToRight;
          slider.minValue = 0f;
          slider.maxValue = 1f;
          slider.SetValueWithoutNotify(Cum(idx));

          slider.onValueChanged.AddListener(v =>
          {
              // the drag only moves this boundary: the two packs either
              // side trade share, everything else keeps its band
              float low = Cum(idx - 1) + MinShare;
              float high = Cum(idx + 1) - MinShare;
              v = Mathf.Clamp(v, low, Mathf.Max(low, high));

              float pair = weights[idx] + weights[idx + 1];
              weights[idx] = v - Cum(idx - 1);
              weights[idx + 1] = pair - weights[idx];

              slider.SetValueWithoutNotify(v);
              LayoutBands();
              // cheap: retag the already-loaded packs, no texture reload
              _catalog.ApplyWeights(sel, weights);
              UpdateLabel();
          });
      }

      mixLabel = _ui.MakeText(_packListRoot, "MixLabel", "", 15, _theme.DimText,
          TextAnchor.UpperLeft).GetComponent<Text>();
      mixLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
      mixLabel.verticalOverflow = VerticalWrapMode.Overflow;
      UpdateLabel();

      // pets keep the pack they spawned as; this re-rolls them all so a
      // mix change shows on the flock that's already out
      var respawn = _ui.MakeButton(_packListRoot, "Respawn", "Respawn pets", 17,
          () =>
          {
              _pets.RerollPets();
              shownCounts = (int[])counts.Clone();
              UpdateLabel(); // back to the plain button
          });
      respawn.gameObject.AddComponent<LayoutElement>().minHeight = 36;
      respawnImg = respawn.GetComponent<Image>();
      respawnText = respawn.GetComponentInChildren<Text>();
      UpdateLabel(); // paints the button for the state it was built in
  }

  // Unity's built-in round knob, for the sprite handles. Not guaranteed
  // to be in every build's resources — a square handle is a fine fallback.
  private Sprite _knob;
  private bool _knobTried;

  private Sprite KnobSprite()
  {
      if (!_knobTried)
      {
          _knobTried = true;
          try { _knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd"); }
          catch { _knob = null; }
      }
      return _knob;
  }

  // ---- per-file sprite role editor -----------------------------------

  // Role ids (as saved in sprite-roles.txt) with the blurb shown in the
  // role picker. Order = picker order.
  private static readonly string[][] RoleInfo =
  {
      new[] { "walk",     "walking animation" },
      new[] { "sit",      "sitting" },
      new[] { "fall",     "gentle falling" },
      new[] { "falling",  "fast / scared plummet" },
      new[] { "climb",    "climbing walls" },
      new[] { "ceiling",  "hanging from the ceiling" },
      new[] { "jump",     "hopping onto tiles / planets" },
      new[] { "idle",     "standing around" },
      new[] { "wave",     "waving at another pet after bumping into it" },
      new[] { "happy",    "celebrates level clears" },
      new[] { "shock",    "Too Early / Too Late reaction" },
      new[] { "dead",     "death & miss reaction" },
      new[] { "bonk",     "crashes & planet hits" },
      new[] { "panic",    "tile scrolling away / planet incoming" },
      new[] { "edge",     "peering over a ledge" },
  };

  private static readonly PackBehavior[] EditorBehaviors =
  {
      PackBehavior.Walk,
      PackBehavior.Sit,
      PackBehavior.Idle,
      PackBehavior.Jump,
      PackBehavior.Climb,
      PackBehavior.Ceiling,
      PackBehavior.Edge,
      PackBehavior.Wave,
      PackBehavior.Panic,
      PackBehavior.Bonk,
      PackBehavior.Judgement,
      PackBehavior.Celebrate,
      PackBehavior.Death,
  };

  private static readonly string[][] BehaviorInfo =
  {
      new[] { "Walking", "move across the floor and rides" },
      new[] { "Sitting", "take seated breaks" },
      new[] { "Standing idle", "pause using idle art" },
      new[] { "Jumping", "hop onto tiles, decorations, planets and the ceiling" },
      new[] { "Wall climbing", "grab and climb the side walls" },
      new[] { "Ceiling hanging", "cling to the top edge" },
      new[] { "Edge peeking", "stop and look over ledges" },
      new[] { "Waving", "greet another pet after a bump" },
      new[] { "Panic", "sprint and react to incoming danger" },
      new[] { "Bonk reaction", "show bonk art after crashes" },
      new[] { "Judgement reaction", "react to Too Early and Too Late" },
      new[] { "Clear celebration", "bounce and cheer after a clear" },
      new[] { "Death reaction", "faint when the player dies" },
  };

  private void BuildSpriteEditor()
  {
      // header: back button + pack name
      var head = _ui.AddRow(_packListRoot, 34);
      var back = _ui.MakeButton(head, "Back", "◀ Packs", 15, () =>
      {
          if (_rolePickFile != null) _rolePickFile = null;
          else _editingPack = null;
          RebuildPackList();
      });
      var backLe = back.gameObject.AddComponent<LayoutElement>();
      backLe.preferredWidth = 110;
      backLe.minHeight = 34;

      var title = _ui.MakeText(head, "Title",
          _rolePickFile != null
              ? "What should  <b>" + _rolePickFile + ".png</b>  do?"
              : _catalog.DisplayName(_editingPack) + " — behaviors & sprite roles",
          16, _theme.Text, TextAnchor.MiddleLeft);
      title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

      // the editor reads/writes THIS pack's role file; SpriteConfig is a
      // single static map, and loading several packs leaves it pointed
      // at whichever loaded last — re-aim it at the pack being edited
      var art = _catalog.Find(_editingPack);
      string dir = art != null ? art.ImageDir : "";
      _roles.Load(dir);

      if (_rolePickFile != null) { BuildRolePicker(_rolePickFile); return; }
      string[] files;
      try
      {
          files = System.IO.Directory.GetFiles(dir, "*.png");
      }
      catch { files = new string[0]; }

      var names = new List<string>(files.Length);
      foreach (string f in files)
          names.Add(System.IO.Path.GetFileNameWithoutExtension(f));
      names.Sort(_roles.CompareNatural);

      _ui.AddSection(_packListRoot, "BEHAVIORS");
      _ui.AddHelp(_packListRoot,
          "These switches affect only this sprite pack. Changes apply to pets immediately.");
      for (int i = 0; i < EditorBehaviors.Length; i++)
      {
          PackBehavior behavior = EditorBehaviors[i];
          bool enabled = _roles.BehaviorEnabled(behavior);
          string titleText = BehaviorInfo[i][0];
          string blurb = BehaviorInfo[i][1];
          var behaviorBtn = _ui.MakeButton(_packListRoot, "Behavior",
              (enabled ? "✓  " : "     ") + titleText
              + "  <color=#9aa0ac>— " + blurb + "</color>",
              16, () =>
              {
                  _roles.SetBehavior(behavior,
                      !_roles.BehaviorEnabled(behavior));
                  _pets.ApplySelectedPacks();
                  RebuildPackList();
              });
          var behaviorLe = behaviorBtn.gameObject.AddComponent<LayoutElement>();
          behaviorLe.minHeight = 32;
          behaviorBtn.GetComponent<Image>().color = enabled ? _theme.AccentDim : _theme.PanelBackground;
          var behaviorText = behaviorBtn.GetComponentInChildren<Text>();
          behaviorText.alignment = TextAnchor.MiddleLeft;
          behaviorText.color = enabled ? _theme.Text : _theme.DimText;
      }

      _ui.AddSection(_packListRoot, "SPRITE ART");
      if (names.Count == 0)
      {
          _ui.AddHelp(_packListRoot, "No PNG files found in this pack.");
          return;
      }

      // pack-wide option (a plain button, not AddToggle: this list is
      // rebuilt constantly and _dynamic entries would outlive their rows)
      bool pingPong = _roles.PingPong;
      var ppBtn = _ui.MakeButton(_packListRoot, "PingPong",
          (pingPong ? "✓  " : "     ") + "Ping-pong walk" +
          "  <color=#9aa0ac>— 1·2·3·4·5·4·3·2·1 instead of looping</color>",
          16, () =>
          {
              _roles.SetPingPong(!_roles.PingPong);
              _pets.ApplySelectedPacks(); // packs re-read the option
              RebuildPackList();
          });
      var ppLe = ppBtn.gameObject.AddComponent<LayoutElement>();
      ppLe.minHeight = 34;
      ppBtn.GetComponent<Image>().color = pingPong ? _theme.AccentDim : _theme.PanelBackground;
      var ppTxt = ppBtn.GetComponentInChildren<Text>();
      ppTxt.alignment = TextAnchor.MiddleLeft;
      ppTxt.color = pingPong ? _theme.Text : _theme.DimText;

      foreach (string file in names)
      {
          string captured = file;
          var row = _ui.AddRow(_packListRoot, 40);

          // thumbnail
          var thumbGo = _ui.NewRect("Thumb", row);
          var thumbLe = thumbGo.gameObject.AddComponent<LayoutElement>();
          thumbLe.preferredWidth = 36;
          thumbLe.preferredHeight = 36;
          var thumbImg = thumbGo.gameObject.AddComponent<Image>();
          var sprite = _loader.LoadThumbnail(dir, file);
          if (sprite != null)
          {
              _resources.Own(sprite);
              thumbImg.sprite = sprite;
              thumbImg.preserveAspect = true;
          }
          else thumbImg.color = new Color(1, 1, 1, 0.05f);
          thumbImg.raycastTarget = false;

          var nameText = _ui.MakeText(row, "Name", file + ".png", 16, _theme.Text,
              TextAnchor.MiddleLeft);
          nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

          // current role button — opens the picker
          string role = _roles.RoleOf(file);
          string label;
          Color labelColor;
          if (role == _roles.DisabledRole)
          {
              label = "disabled";
              labelColor = new Color(1f, 0.55f, 0.45f);
          }
          else if (role != null)
          {
              label = role;
              labelColor = _theme.Accent;
          }
          else
          {
              string auto = _roles.AutoRoleOf(file);
              label = "auto (" + (auto ?? "unused") + ")";
              labelColor = _theme.DimText;
          }
          var roleBtn = _ui.MakeButton(row, "Role", label, 15, () =>
          {
              _rolePickFile = captured;
              RebuildPackList();
          });
          var roleLe = roleBtn.gameObject.AddComponent<LayoutElement>();
          roleLe.preferredWidth = 220;
          roleLe.minHeight = 34;
          roleBtn.GetComponentInChildren<Text>().color = labelColor;
      }
  }

  private void BuildRolePicker(string file)
  {
      void Pick(string role)
      {
          _roles.SetRole(file, role);
          _pets.ApplySelectedPacks(); // reload frames + refresh warning
          _rolePickFile = null;
          RebuildPackList();
      }

      string current = _roles.RoleOf(file); // null = auto

      void RoleButton(string role, string title, string blurb, bool selected)
      {
          var btn = _ui.MakeButton(_packListRoot, "RoleOpt",
              (selected ? "✓  " : "") + title +
              "  <color=#9aa0ac>— " + blurb + "</color>",
              16, () => Pick(role));
          var le = btn.gameObject.AddComponent<LayoutElement>();
          le.minHeight = 32;
          btn.GetComponent<Image>().color = selected ? _theme.AccentDim : _theme.PanelBackground;
          var txt = btn.GetComponentInChildren<Text>();
          txt.alignment = TextAnchor.MiddleLeft;
          txt.color = selected ? _theme.Text : _theme.DimText;
      }

      string auto = _roles.AutoRoleOf(file);
      RoleButton("auto", "auto",
          "let the filename decide (" + (auto ?? "currently unused") + ")",
          current == null);
      RoleButton(_roles.DisabledRole, "disabled",
          "never show this sprite", current == _roles.DisabledRole);
      foreach (var info in RoleInfo)
          RoleButton(info[0], info[0], info[1], current == info[0]);
  }

  // Warn when a selected pack lacks reaction art — those reactions are
  // simply skipped (for the pets wearing that pack) rather than shown
  // with substitute sprites.
  private void RefreshPackWarning()
  {
      if (_packWarning == null) return;
      var lines = new List<string>();
      foreach (var art in _catalog.LoadedPacks)
      {
          var missing = art.MissingReactions();
          if (missing.Count == 0) continue;
          lines.Add("\"" + _catalog.DisplayName(art.Pack)
              + "\" is missing sprites — these stay disabled until the pack has them:\n  • "
              + string.Join("\n  • ", missing.ToArray()));
      }
      _packWarning.gameObject.SetActive(lines.Count > 0);
      if (lines.Count == 0) return;
      _packWarning.text = string.Join("\n\n", lines.ToArray());
  }


  public void OnShown() => RebuildPackList();
  public void Refresh() => RebuildPackList();

  public void Dispose()
  {
    _resources.Dispose();
    _packListRoot = null;
    _packWarning = null;
    _editingPack = null;
    _rolePickFile = null;
  }
}
