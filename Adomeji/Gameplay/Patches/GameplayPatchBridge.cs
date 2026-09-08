using System;
using Adomeji.Gameplay.Events;
using Adomeji.Shared.Diagnostics;
using UnityEngine;

namespace Adomeji.Gameplay.Patches;

internal static class GameplayPatchBridge
{
  internal static bool SuppressGameMouse { get; set; }

  internal static bool SkipWhileMouseSuppressed()
  {
    return !SuppressGameMouse;
  }

  internal static void MouseButtonPostfix(int button, ref bool __result)
  {
    if (SuppressGameMouse && button == 0)
      __result = false;
  }

  private static IGameplayEventSink _events;
  private static IModLogger _logger;

  private static readonly RunJudgementTracker Judgements = new RunJudgementTracker();
  private static float _lastFailureTime = -10f;

  public static void Connect(
    IGameplayEventSink events,
    IModLogger logger
  )
  {
    _events = events;
    _logger = logger;
    Judgements.Reset();
  }

  public static void Disconnect()
  {
    SuppressGameMouse = false;
    _events = null;
    _logger = null;
    Judgements.Reset();
  }

  internal static void LevelStartedPostfix()
  {
    try
    {
      Judgements.Reset();
      _events?.OnLevelStarted();
    }
    catch (Exception exception)
    {
      _logger?.Error(nameof(LevelStartedPostfix), exception);
    }
  }

  internal static void JudgementPostfix(HitMargin hit)
  {
    try
    {
      JudgementKind judgement = JudgementMapper.FromName(hit.ToString());

      if (judgement == JudgementKind.Auto)
        return;

      Judgements.Record(judgement);

      _events?.OnJudgement(judgement);
    }
    catch (Exception exception)
    {
      _logger?.Error(nameof(JudgementPostfix), exception);
    }
  }

  internal static void OverpressPostfix(HitMargin hitMargin)
  {
    try
    {
      if (hitMargin != HitMargin.OverPress)
        return;

      Judgements.Record(JudgementKind.OverPress);
      _events?.OnJudgement(JudgementKind.OverPress);
    }
    catch (Exception exception)
    {
      _logger?.Error(nameof(OverpressPostfix), exception);
    }
  }

  internal static void LevelClearedPostfix()
  {
    try
    {
      bool purePerfect = Judgements.IsPurePerfect;

      _events?.OnLevelCleared(purePerfect);
      Judgements.Reset();
    }
    catch (Exception exception)
    {
      _logger?.Error(nameof(LevelClearedPostfix), exception);
    }
  }

  internal static void LevelFailedPostfix()
  {
    try
    {
      float now = Time.realtimeSinceStartup;

      if (now - _lastFailureTime < 1f)
        return;

      _lastFailureTime = now;
      _events?.OnLevelFailed();
    }
    catch (Exception exception)
    {
      _logger?.Error(nameof(LevelFailedPostfix), exception);
    }
  }

}
