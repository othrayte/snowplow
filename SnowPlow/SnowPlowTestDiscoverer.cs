using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SnowPlow
{
    [FileExtension(".exe")]
    [DefaultExecutorUri(SnowPlowTestExecutor.ExecutorUriString)]
    public class SnowPlowTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.LookingForSnowN, sources.Count()));
            GetTests(sources, discoveryContext, logger, discoverySink);
            logger.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + strings.FinishedLooking);
        }

        internal static IEnumerable<TestCase> GetTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            XmlTestCaseReader testReader = new XmlTestCaseReader(discoverySink);

            foreach (string source in sources)
            {
                try
                {
                    FileInfo file = new FileInfo(source);
                    if (!file.Exists)
                    {
                        logger.SendMessage(TestMessageLevel.Warning, strings.SnowPlow_ + string.Format(strings.UnknownFileX, source));
                    }

                    Binary settings = Configuration.FindConfiguration(file);

                    if (settings == null)
                    {
                        logger.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXNotListed, source));
                        continue;
                    }

                    if (!settings.Enable)
                    {
                        logger.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.SkipXDisabled, source));
                        continue;
                    }

                    logger.SendMessage(TestMessageLevel.Informational, strings.SnowPlow_ + string.Format(strings.LookingInX, source));

                    Process process = new Process(file, settings);
                    // Start the process, Call WaitForExit and then the using statement will close.
                    using (System.Diagnostics.Process unittestProcess = process.listTests())
                    {
                        string rawContent = unittestProcess.StandardOutput.ReadToEnd();

                        testReader.read(source, XmlWasher.clean(rawContent));

                        int timeout = 10000;
                        unittestProcess.WaitForExit(timeout);

                        if (!unittestProcess.HasExited)
                        {
                            unittestProcess.Kill();
                            logger.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.TimoutInX, source));
                            continue;
                        }

                        if (unittestProcess.ExitCode < 0)
                        {
                            logger.SendMessage(TestMessageLevel.Error, strings.SnowPlow_ + string.Format(strings.XReturnedErrorCodeY, source, unittestProcess.ExitCode));
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log error.
                    string message = strings.SnowPlow_ + string.Format(strings.ExceptionThrownMsg, e.ToString());
                    Debug.Assert(false, message);
                    logger.SendMessage(TestMessageLevel.Error, message);
                }
            }

            return testReader.TestCases;
        }

    }
}
