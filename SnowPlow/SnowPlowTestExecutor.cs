using EnsureThat;
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

        private void RunTests(String source, IEnumerable<TestCase> tests, IFrameworkHandle frameworkHandle, IRunContext runContext)
        {
            if (m_cancelled)
            {
                return;
            }

            FileInfo file = new FileInfo(source);
            if (!file.Exists)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, strings.SnowPlow_ + string.Format(strings.UnknownFileX, source));
            }

            Container settings = PlowConfiguration.FindConfiguration(file);

            if (settings == null)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXNotListed, source));
                return;
            }

            if (!settings.Enable)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXDisabled, source));
                return;
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.PlowingInX, source));

            // Start the process, Call WaitForExit and then the using statement will close.
            Process process = new Process(file, settings);

            if (runContext.IsBeingDebugged)
            {
                process.DebugTests(frameworkHandle).WaitForExit();
            }
            else
            {
                using (System.Diagnostics.Process unittestProcess = process.ExecuteTests())
                {
                    string rawContent = unittestProcess.StandardOutput.ReadToEnd();

                    XmlTestResultReader testReader = new XmlTestResultReader(frameworkHandle);
                    testReader.read(tests, XmlWasher.Clean(rawContent));

                    int timeout = 10000;
                    unittestProcess.WaitForExit(timeout);

                    if (!unittestProcess.HasExited)
                    {
                        unittestProcess.Kill();
                        frameworkHandle.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.TimoutInX, source));
                        return;
                    }

                    if (unittestProcess.ExitCode < 0)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.XReturnedErrorCodeY, source, unittestProcess.ExitCode));
                        return;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Ensure.That(() => tests).IsNotNull();
            Ensure.That(() => runContext).IsNotNull();
            Ensure.That(() => frameworkHandle).IsNotNull();

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
                try
                {
                    RunTests(source, sources[source], frameworkHandle, runContext);
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
