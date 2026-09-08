using System.Collections.Generic;
using Adomeji.Sprites.Loading;

namespace Adomeji.Sprites.Catalog;

internal sealed class SpritePackCatalog : ISpritePackCatalog
{
  public IList<PackArt> LoadedPacks => PlaceholderArt.LoadedPacks;
  public string[] Scan() => PlaceholderArt.ScanPacks();
  public PackArt Find(string id) => PlaceholderArt.PackFor(id);
  public string DisplayName(string id) => PlaceholderArt.DisplayName(id);
  public void ApplySelection(List<string> packs) => PlaceholderArt.ApplyPacks(packs);
  public void ApplyWeights(List<string> packs, List<float> weights) =>
    PlaceholderArt.ApplyWeights(packs, weights);
}
