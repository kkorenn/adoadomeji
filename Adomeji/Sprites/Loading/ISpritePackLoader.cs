using UnityEngine;

namespace Adomeji.Sprites.Loading;

internal interface ISpritePackLoader
{
  string RootDirectory { get; }
  string PackDirectory(string pack);
  Sprite LoadThumbnail(string directory, string baseName);
}
