
namespace SnowPlow
{
    public static class IglooSpecNameFormatter
    {
        public static string buildDisplayName(string className, string name)
        {
            return (className + " " + name).Replace("_", " ").Replace("::", " ");
        }

        public static string buildDisplayName(string name)
        {
            return name.Replace("_", " ").Replace("::", " ");
        }

        public static string buildTestName(string className, string name)
        {
            return className + "::" + name;
        }

        public static string buildTestName(string name)
        {
            return name;
        }
    }
}
