using System;

namespace Adomeji.Shared.Text;

internal static class NaturalStringComparer
{
  public static int Compare(string a, string b)
  {
    SplitTrailingNumber(a, out string leftStem, out int leftNumber);
    SplitTrailingNumber(b, out string rightStem, out int rightNumber);

    int stemComparison = string.Compare(leftStem, rightStem, StringComparison.OrdinalIgnoreCase);
    if (stemComparison != 0) return stemComparison;
    if (leftNumber != rightNumber) return leftNumber.CompareTo(rightNumber);
    return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
  }

  private static void SplitTrailingNumber(string value, out string stem, out int number)
  {
    int end = value.Length;
    while (end > 0 && value[end - 1] >= '0' && value[end - 1] <= '9') end--;
    stem = value.Substring(0, end);
    number = 0;
    if (end < value.Length) int.TryParse(value.Substring(end), out number);
  }
}
