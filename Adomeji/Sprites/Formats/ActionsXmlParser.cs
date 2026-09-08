using System.Collections.Generic;

namespace Adomeji.Sprites.Formats;

internal static class ActionsXmlParser
{
  public static Dictionary<string, string[]> Parse(string text)
  {
    var result = new Dictionary<string, string[]>();
    var names = new List<string>();
    var images = new List<List<string>>();

    int position = 0;
    while (true)
    {
      int open = text.IndexOf('<', position);
      if (open < 0) break;
      int close = text.IndexOf('>', open + 1);
      if (close < 0) break;
      string tag = text.Substring(open + 1, close - open - 1);
      position = close + 1;
      if (tag.Length == 0 || tag[0] == '?' || tag[0] == '!') continue;

      if (tag[0] == '/')
      {
        if (TagName(tag.Substring(1)) == "Action" && names.Count > 0)
        {
          int last = names.Count - 1;
          string name = names[last];
          List<string> poses = images[last];
          if (name.Length > 0 && poses.Count > 0 && !result.ContainsKey(name))
            result[name] = poses.ToArray();
          names.RemoveAt(last);
          images.RemoveAt(last);
        }
        continue;
      }

      string element = TagName(tag);
      bool selfClosing = tag[tag.Length - 1] == '/';
      if (element == "Action")
      {
        if (selfClosing) continue;
        names.Add(Attribute(tag, "Name") ?? "");
        images.Add(new List<string>());
      }
      else if (element == "Pose" && names.Count > 0)
      {
        string image = Attribute(tag, "Image");
        if (image != null)
          images[images.Count - 1].Add(BaseName(image));
      }
    }

    return result;
  }

  private static string TagName(string tag)
  {
    int end = 0;
    while (end < tag.Length && !IsSpace(tag[end]) && tag[end] != '/') end++;
    return tag.Substring(0, end);
  }

  private static string Attribute(string tag, string name)
  {
    int position = 0;
    while (true)
    {
      position = tag.IndexOf(name, position);
      if (position < 0) return null;
      int after = position + name.Length;
      bool boundary = position > 0 && IsSpace(tag[position - 1]);
      int valueStart = after;
      while (valueStart < tag.Length && IsSpace(tag[valueStart])) valueStart++;
      if (boundary && valueStart < tag.Length && tag[valueStart] == '=')
      {
        valueStart++;
        while (valueStart < tag.Length && IsSpace(tag[valueStart])) valueStart++;
        if (valueStart < tag.Length && (tag[valueStart] == '"' || tag[valueStart] == '\''))
        {
          char quote = tag[valueStart++];
          int valueEnd = tag.IndexOf(quote, valueStart);
          return valueEnd >= 0 ? tag.Substring(valueStart, valueEnd - valueStart) : null;
        }
      }
      position = after;
    }
  }

  private static bool IsSpace(char value) =>
    value == ' ' || value == '\t' || value == '\n' || value == '\r';

  private static string BaseName(string path)
  {
    int separator = -1;
    for (int i = path.Length - 1; i >= 0; i--)
      if (path[i] == '/' || path[i] == '\\') { separator = i; break; }
    string file = separator >= 0 ? path.Substring(separator + 1) : path;
    int extension = file.LastIndexOf('.');
    return extension > 0 ? file.Substring(0, extension) : file;
  }
}
