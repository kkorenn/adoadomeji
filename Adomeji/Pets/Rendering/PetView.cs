using UnityEngine;
using UnityEngine.UI;

namespace Adomeji.Pets.Rendering;

internal sealed class PetView
{
  private readonly RectTransform _transform;
  private readonly Image _image;
  private int _spriteKey = -1;
  private float _flip;
  private float _rotation = float.NaN;
  private float _size = -1f;
  private Vector2 _position = new Vector2(float.NaN, float.NaN);
  private Color _color = Color.white;

  public PetView(RectTransform transform, Image image)
  {
    _transform = transform;
    _image = image;
    _image.raycastTarget = false;
    _image.maskable = false;
    _transform.anchorMin = Vector2.zero;
    _transform.anchorMax = Vector2.zero;
    _transform.pivot = new Vector2(0.5f, 0.5f);
  }

  public bool IsAlive => _transform != null && _image != null;

  public void InvalidateSprite() => _spriteKey = -1;

  public void SetColor(Color color)
  {
    if (color == _color) return;
    _color = color;
    _image.color = color;
  }

  public void ApplyLayout(Vector2 position, float rotation, float size, float flip)
  {
    if (size != _size)
    {
      _size = size;
      _transform.sizeDelta = new Vector2(size, size);
    }

    if (position != _position)
    {
      _position = position;
      _transform.anchoredPosition = position;
    }

    if (rotation != _rotation)
    {
      _rotation = rotation;
      _transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    if (flip != _flip)
    {
      _flip = flip;
      _transform.localScale = new Vector3(flip, 1f, 1f);
    }
  }

  public void ApplySprite(int key, Sprite sprite)
  {
    if (key == _spriteKey) return;
    _spriteKey = key;
    _image.sprite = sprite;
  }
}
