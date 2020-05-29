using System.Collections.Generic;

namespace Remotion.BuildScript
{
  public static class StringKeyValuePairListExtensions
  {
    public static bool Remove (this IList<KeyValuePair<string, string>> list, string sameKeyValue)
    {
      return list.Remove (new KeyValuePair<string, string> (sameKeyValue, sameKeyValue));
    }
  }
}