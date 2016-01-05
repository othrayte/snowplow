using EnsureThat;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
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
            IEnumerable<TestCase> tests = SnowPlowTestDiscoverer.GetTests(sources, new Logger(frameworkHandle), null);

            RunTests(tests, runContext, frameworkHandle);
        }

        private void RunTests(String source, IEnumerable<TestCase> tests, IFrameworkHandle frameworkHandle, IRunContext runContext)
        {
            Logger logger = new Logger(frameworkHandle);

            if (m_cancelled)
            {
                return;
            }

            FileInfo file = new FileInfo(source);
            if (!file.Exists)
            {
                logger.WriteWarning(strings.UnknownFileX, source);
            }

            Container settings = PlowConfiguration.FindConfiguration(file);

            if (settings == null)
            {
                logger.WriteInformation(strings.SkipXNotListed, source);
                return;
            }

            if (!settings.Enable)
            {
                logger.WriteInformation(strings.SkipXDisabled, source);
                return;
            }

            logger.WriteInformation(strings.PlowingInX, source);

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
                        logger.WriteError(strings.TimoutInX, source);
                        return;
                    }

                    if (unittestProcess.ExitCode < 0)
                    {
                        logger.WriteError(strings.XReturnedErrorCodeY, source, unittestProcess.ExitCode);
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

            Logger logger = new Logger(frameworkHandle);

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
                catch (Exception exception)
                {
                    // Log exception as error.
                    logger.WriteException(exception);
                }

            }
        }

        public void Cancel()
        {
            m_cancelled = true;
        }
    }
}
