using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SnowPlow
{

    [ExtensionUri(SnowPlowTestExecutor.ExecutorUriString)]
    public class SnowPlowTestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://SnowPlowTestExecutor";

        public static readonly Uri ExecutorUri = new Uri(SnowPlowTestExecutor.ExecutorUriString);

        private bool m_cancelled;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            IEnumerable<TestCase> tests = SnowPlowTestDiscoverer.GetTests(sources, runContext, frameworkHandle, null);

            RunTests(tests, runContext, frameworkHandle);

        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            XmlTestResultReader testReader = new XmlTestResultReader(frameworkHandle);
            m_cancelled = false;

            Dictionary<String, LinkedList<TestCase>> sources = new Dictionary<String, LinkedList<TestCase>>();
            foreach (TestCase test in tests)
            {
                if (!sources.ContainsKey(test.Source))
                    sources.Add(test.Source, new LinkedList<TestCase>());
                sources[test.Source].AddLast(test);
            }

            foreach (String source in sources.Keys)
            {
                if (m_cancelled)
                {
                    break;
                }

                try
                {
                    FileInfo file = new FileInfo(source);
                    if (!file.Exists)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Warning, string.Format("SnowPlow: Asked to plow unknown file {0}", source));
                    }

                    Binary settings = Configuration.FindConfiguration(file);

                    if (settings == null)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Skipping source {0}, not listed in a plow definition", source));
                        continue;
                    }

                    if (!settings.Enable)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Skipping source {0}, disabled in plow definition", source));
                        continue;
                    }

                    frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Plowing in {0}", source));

                    // Start the process, Call WaitForExit and then the using statement will close.
                    Process process = new Process(file, settings);

                    if (runContext.IsBeingDebugged)
                    {
                        process.debugTests(frameworkHandle).WaitForExit();
                    }
                    else
                    {
                        using (System.Diagnostics.Process unittestProcess = process.executeTests())
                        {
                            string rawContent = unittestProcess.StandardOutput.ReadToEnd();

                            testReader.read(sources[source], XmlWasher.clean(rawContent));

                            int timeout = 10000;
                            unittestProcess.WaitForExit(timeout);

                            if (!unittestProcess.HasExited)
                            {
                                unittestProcess.Kill();
                                frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Ran out of time plowing tests in {0}, test process has been killed.", source));
                                continue;
                            }

                            if (unittestProcess.ExitCode < 0)
                            {
                                frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Broke plow, {0} returned exit code {1}", source, unittestProcess.ExitCode));
                                continue;
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    // Log error.
                    string message = string.Format("SnowPlow: Ran of the road. {0}", e.ToString());
                    Debug.Assert(false, message);
                    frameworkHandle.SendMessage(TestMessageLevel.Error, message);
                }

            }
        }

        public void Cancel()
        {
            m_cancelled = true;
        }
    }
}
