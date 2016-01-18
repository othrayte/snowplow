using EnsureThat;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
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
            GetTests(sources, new Logger(logger), discoverySink);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static IEnumerable<TestCase> GetTests(IEnumerable<string> sources, Logger logger, ITestCaseDiscoverySink discoverySink)
        {
            Ensure.That(() => logger).IsNotNull();

            logger.WriteInformation(strings.LookingForSnowN, sources.Count());
            
            XmlTestCaseReader testReader = new XmlTestCaseReader(discoverySink);

            foreach (string source in sources)
            {
                try
                {
                    FileInfo file = new FileInfo(source);
                    if (!file.Exists)
                    {
                        logger.WriteWarning(strings.UnknownFileX, source);
                    }

                    Container settings = PlowConfiguration.FindConfiguration(file);

                    if (settings == null)
                    {
                        logger.WriteWarning(strings.SkipXNotListed, source);
                        continue;
                    }

                    if (!settings.Enable)
                    {
                        logger.WriteWarning(strings.SkipXDisabled, source);
                        continue;
                    }

                    logger.WriteInformation(strings.LookingInX, source);

                    Process process = new Process(file, settings);
                    // Start the process, Call WaitForExit and then the using statement will close.
                    using (System.Diagnostics.Process unittestProcess = process.ListTests())
                    {
                        string rawContent = unittestProcess.StandardOutput.ReadToEnd();

                        int timeout = 10000;
                        unittestProcess.WaitForExit(timeout);

                        if (!unittestProcess.HasExited)
                        {
                            unittestProcess.Kill();
                            logger.WriteError(strings.TimoutInX, source);
                            continue;
                        }

                        if (unittestProcess.ExitCode < 0)
                        {
                            logger.WriteError(strings.XReturnedErrorCodeY, source, unittestProcess.ExitCode);
                            continue;
                        }

                        testReader.Read(source, XmlWasher.Clean(rawContent));
                    }
                }
                catch (Exception exception)
                {
                    // Log exception as error.
                    logger.WriteException(exception);
                }
            }

            logger.WriteInformation(strings.FinishedLooking);

            return testReader.TestCases;
        }

    }
}
