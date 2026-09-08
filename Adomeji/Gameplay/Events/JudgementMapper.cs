namespace Adomeji.Gameplay.Events;

internal static class JudgementMapper
{
  public static JudgementKind FromName(string name)
  {
    switch (name)
    {
      case "Auto": return JudgementKind.Auto;
      case "Perfect": return JudgementKind.Perfect;
      case "TooEarly": return JudgementKind.TooEarly;
      case "TooLate": return JudgementKind.TooLate;
      case "Multipress": return JudgementKind.Multipress;
      case "OverPress": return JudgementKind.OverPress;
      default: return JudgementKind.Unknown;
    }
  }
}
