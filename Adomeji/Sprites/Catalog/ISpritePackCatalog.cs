using System.Collections.Generic;
using Adomeji.Sprites.Loading;

namespace Adomeji.Sprites.Catalog;

internal interface ISpritePackCatalog
{
  IList<PackArt> LoadedPacks { get; }
  string[] Scan();
  PackArt Find(string id);
  string DisplayName(string id);
  void ApplySelection(List<string> packs);
  void ApplyWeights(List<string> packs, List<float> weights);
}
