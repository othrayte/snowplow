using System.Text.RegularExpressions;

namespace SnowPlow
{
    class XmlWasher
    {
        public static string clean(string content)
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
