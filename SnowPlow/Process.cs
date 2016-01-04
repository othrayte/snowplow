using EnsureThat;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SnowPlow
{
    public class Process
    {
        FileInfo File { get; set; }
        Container Settings { get; set; }
        List<String> Arguments { get; set; }

        public Process(FileInfo file, Container settings)
        {
            File = file;
            Settings = settings;
            Arguments = new List<string>();
            Arguments.Add("--output=xunit");
        }

        public ProcessStartInfo StartInfo()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.WorkingDirectory = File.Directory.FullName;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = File.FullName;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = string.Join(" ", Arguments);

            // Add modified env vars
            foreach (EnvironmentVariable var in Settings.EnvironmentVariables)
            {
                if (startInfo.EnvironmentVariables.ContainsKey(var.Name))
                {
                    startInfo.EnvironmentVariables.Remove(var.Name);
                }
                startInfo.EnvironmentVariables.Add(var.Name, Environment.ExpandEnvironmentVariables(var.Value));
            }
            return startInfo;
        }

        public System.Diagnostics.Process ListTests()
        {
            ProcessStartInfo info = StartInfo();
            info.Arguments += " --list";
            return System.Diagnostics.Process.Start(info);
        }

        public System.Diagnostics.Process ExecuteTests()
        {
            return System.Diagnostics.Process.Start(StartInfo());
        }

        public System.Diagnostics.Process DebugTests(IFrameworkHandle frameworkHandle)
        {
            Ensure.That(() => frameworkHandle).IsNotNull();

            ProcessStartInfo info = StartInfo();
            Dictionary<string, string> environment =
                info.EnvironmentVariables.Cast<DictionaryEntry>().ToDictionary(
                    item => item.Key.ToString(),
                    item => item.Value.ToString()
                );
            Debug.Assert(false);
            int pid = frameworkHandle.LaunchProcessWithDebuggerAttached(info.FileName, info.WorkingDirectory, info.Arguments, environment);
            System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(pid);
            return process;
        }
    }
}