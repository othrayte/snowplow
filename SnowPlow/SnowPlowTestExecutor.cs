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
            IEnumerable<TestCase> tests = SnowPlowTestDiscoverer.GetTests(sources, frameworkHandle, null);

            RunTests(tests, runContext, frameworkHandle);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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
                        frameworkHandle.SendMessage(TestMessageLevel.Warning, strings.SnowPlow_ + string.Format(strings.UnknownFileX, source));
                    }

                    Binary settings = Configuration.FindConfiguration(file);

                    if (settings == null)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXNotListed, source));
                        continue;
                    }

                    if (!settings.Enable)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXDisabled, source));
                        continue;
                    }

                    frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.PlowingInX, source));

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
                                frameworkHandle.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.TimoutInX, source));
                                continue;
                            }

                            if (unittestProcess.ExitCode < 0)
                            {
                                frameworkHandle.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.XReturnedErrorCodeY, source, unittestProcess.ExitCode));
                                continue;
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    // Log error.
                    string message = strings.SnowPlow_ + string.Format(strings.ExceptionThrownMsg, e.ToString());
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
