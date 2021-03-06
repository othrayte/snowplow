﻿using System.Text.RegularExpressions;

namespace SnowPlow
{
    public static class XmlWasher
    {
        public static string Clean(string content)
        {
            Regex r = new Regex(@"(<\?xml.*>)", RegexOptions.Singleline);
            Match m = r.Match(content);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            else
            {
                return "";
            }
        }
    }
}
