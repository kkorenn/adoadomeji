using UnityEngine;

namespace Adomeji.Sprites.Loading;

internal sealed class SpritePackLoader : ISpritePackLoader
{
  public string RootDirectory => PlaceholderArt.SpritesRoot();
  public string PackDirectory(string pack) => PlaceholderArt.PackDir(pack);
  public Sprite LoadThumbnail(string directory, string baseName) =>
    PlaceholderArt.LoadThumb(directory, baseName);
}
