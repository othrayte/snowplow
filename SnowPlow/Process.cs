using System;
using System.Diagnostics;
using System.IO;

namespace SnowPlow
{
    class Process
    {
        public static System.Diagnostics.Process forFile(FileInfo file, Binary settings, bool listOnly = false)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = file.FullName;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = listOnly ? "--list --output=xunit" : "--output=xunit";

            // Add modified env vars
            foreach (EnvVar var in settings.EnvVars)
            {
                if (startInfo.EnvironmentVariables.ContainsKey(var.Name))
                {
                    startInfo.EnvironmentVariables.Remove(var.Name);
                }
                startInfo.EnvironmentVariables.Add(var.Name, Environment.ExpandEnvironmentVariables(var.Value));
            }

            return System.Diagnostics.Process.Start(startInfo);
        }
    }
}
