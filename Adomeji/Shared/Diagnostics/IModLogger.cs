using System;

namespace Adomeji.Shared.Diagnostics;

internal interface IModLogger
{
  void Info(string message);
  void Error(string message);
  void Error(string context, Exception exception);
}
