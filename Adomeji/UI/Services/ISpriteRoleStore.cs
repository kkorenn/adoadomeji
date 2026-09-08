using Adomeji.Sprites.Configuration;

namespace Adomeji.UI.Services;

internal interface ISpriteRoleStore
{
  string DisabledRole { get; }
  void Load(string directory);
  bool BehaviorEnabled(PackBehavior behavior);
  void SetBehavior(PackBehavior behavior, bool enabled);
  bool PingPong { get; }
  void SetPingPong(bool enabled);
  string RoleOf(string file);
  string AutoRoleOf(string file);
  void SetRole(string file, string role);
  int CompareNatural(string left, string right);
}

internal sealed class SpriteRoleStore : ISpriteRoleStore
{
  public string DisabledRole => SpriteConfig.Disabled;
  public bool PingPong => SpriteConfig.PingPong;
  public void Load(string directory) => SpriteConfig.Load(directory);
  public bool BehaviorEnabled(PackBehavior behavior) => SpriteConfig.BehaviorEnabled(behavior);
  public void SetBehavior(PackBehavior behavior, bool enabled) => SpriteConfig.SetBehavior(behavior, enabled);
  public void SetPingPong(bool enabled) => SpriteConfig.SetPingPong(enabled);
  public string RoleOf(string file) => SpriteConfig.RoleOf(file);
  public string AutoRoleOf(string file) => SpriteConfig.AutoRoleOf(file);
  public void SetRole(string file, string role) => SpriteConfig.SetRole(file, role);
  public int CompareNatural(string left, string right) => SpriteConfig.CompareNatural(left, right);
}
