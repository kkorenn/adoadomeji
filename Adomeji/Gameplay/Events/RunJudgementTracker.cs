namespace Adomeji.Gameplay.Events;

internal sealed class RunJudgementTracker
{
  private int _totalHits;
  private int _nonPerfectHits;

  public bool IsPurePerfect => _totalHits > 0 && _nonPerfectHits == 0;

  public void Record(JudgementKind judgement)
  {
    if (judgement == JudgementKind.Auto)
      return;

    _totalHits++;
    if (judgement != JudgementKind.Perfect)
      _nonPerfectHits++;
  }

  public void Reset()
  {
    _totalHits = 0;
    _nonPerfectHits = 0;
  }
}
