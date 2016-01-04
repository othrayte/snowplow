
using EnsureThat;
namespace SnowPlow
{
    public static class IglooSpecNameFormatter
    {
        public static string BuildDisplayName(string className, string name)
        {
            return (className + " " + name).Replace("_", " ").Replace("::", " ");
        }

        public static string BuildDisplayName(string name)
        {
            Ensure.That(() => name).IsNotNullOrWhiteSpace();
            return name.Replace("_", " ").Replace("::", " ");
        }

        public static string BuildTestName(string className, string name)
        {
            return className + "::" + name;
        }

        public static string BuildTestName(string name)
        {
            return name;
        }
    }
}
