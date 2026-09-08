using System;
using UnityModManagerNet;

namespace Adomeji.Shared.Diagnostics;

internal sealed class UmmModLogger : IModLogger
{
  private readonly UnityModManager.ModEntry.ModLogger _logger;

  public UmmModLogger(UnityModManager.ModEntry.ModLogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public void Info(string message)
  {
    _logger.Log(message);
  }

  public void Error(string message)
  {
    _logger.Error(message);
  }

  public void Error(string context, Exception exception)
  {
    _logger.Error("[" + context + "]" + exception);
  }
}
