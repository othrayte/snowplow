using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace SnowPlow
{
    [FileExtension(".exe")]
    [DefaultExecutorUri(SnowPlowTestExecutor.ExecutorUriString)]
    public class SnowPlowTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Looking for snow in {0} places", sources.Count()));
            GetTests(sources, discoveryContext, logger, discoverySink);
            logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Finished looking for snow"));
        }

        internal static IEnumerable<TestCase> GetTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<TestCase> tests = new List<TestCase>();

            foreach (string source in sources)
            {
                try
                {
                    FileInfo file = new FileInfo(source);
                    if (!file.Exists)
                    {
                        logger.SendMessage(TestMessageLevel.Warning, string.Format("SnowPlow: Asked to plow unknown file {0}", source));
                    }

                    Binary settings = Configuration.FindConfiguration(file);

                    if (settings == null)
                    {
                        logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Skipping source {0}, not listed in a plow definition", source));
                        continue;
                    }

                    if (!settings.Enable)
                    {
                        logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Skipping source {0}, disabled in plow definition", source));
                        continue;
                    }

                    logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Looking in {0}", source));

                    // Start the process, Call WaitForExit and then the using statement will close.
                    using (System.Diagnostics.Process unittestProcess = Process.forFile(file, settings, true))
                    {
                        string output = "";
                        using (StreamReader reader = unittestProcess.StandardOutput)
                        {
                            output = reader.ReadToEnd();
                        }
                        if (unittestProcess.ExitCode < 0)
                        {
                            logger.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Broke plow, {0} returned exit code {1}", source, unittestProcess.ExitCode));
                            logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: {0}:{1} >>> {2} <<<", source, unittestProcess.ExitCode, output));
                            continue;
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(output);

                        var testNodes = doc.SelectNodes("//testsuite/testcase");
                        foreach (XmlNode testNode in testNodes)
                        {
                            XmlAttribute nameAttribute = testNode.Attributes["name"];
                            XmlAttribute classnameAttribute = testNode.Attributes["classname"];
                            if (nameAttribute != null && !String.IsNullOrWhiteSpace(nameAttribute.Value))
                            {
                                string name = nameAttribute.Value;

                                if (classnameAttribute != null && !String.IsNullOrWhiteSpace(classnameAttribute.Value))
                                {
                                    name = classnameAttribute.Value + "::" + name;
                                }

                                var testCase = new TestCase(name, SnowPlowTestExecutor.ExecutorUri, source);

                                tests.Add(testCase);

                                if (discoverySink != null)
                                {
                                    discoverySink.SendTestCase(testCase);
                                }

                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log error.
                    logger.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Exception thrown through windscreen: {0}", e.Message));
                }
            }

            return tests;
        }
    }
}
