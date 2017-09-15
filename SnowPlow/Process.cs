using EnsureThat;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

		public ProcessStartInfo StartInfo(Logger logger)
		{
			Ensure.That(() => logger).IsNotNull();

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
				String expandedValue = Environment.ExpandEnvironmentVariables(var.Value);
				Regex unexpandedEnvVar = new Regex(@"%[a-zA-Z0-9_]+%", RegexOptions.Singleline);
				if (unexpandedEnvVar.IsMatch(expandedValue))
				{
					logger.WriteWarning(strings.UnexpandedEnvVarInS, var.Name, expandedValue);
				}
				startInfo.EnvironmentVariables.Add(var.Name, expandedValue);

            }
            return startInfo;
        }

		public System.Diagnostics.Process ListTests(Logger logger)
        {
            ProcessStartInfo info = StartInfo(logger);
            info.Arguments += " --list";
            return System.Diagnostics.Process.Start(info);
        }

		public System.Diagnostics.Process ExecuteTests(Logger logger)
        {
			return System.Diagnostics.Process.Start(StartInfo(logger));
        }

		public System.Diagnostics.Process DebugTests(IFrameworkHandle frameworkHandle, Logger logger)
        {
            Ensure.That(() => frameworkHandle).IsNotNull();

			ProcessStartInfo info = StartInfo(logger);
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