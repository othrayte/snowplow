using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                    Process process = new Process(file, settings);
                    // Start the process, Call WaitForExit and then the using statement will close.
                    using (System.Diagnostics.Process unittestProcess = process.listTests())
                    {
                        using (XmlReader reader = XmlReader.Create(unittestProcess.StandardOutput))
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(reader);

                            var testNodes = doc.SelectNodes("//testsuite/testcase");
                            foreach (XmlNode testNode in testNodes)
                            {
                                XmlAttribute nameAttribute = testNode.Attributes["name"];
                                XmlAttribute classnameAttribute = testNode.Attributes["classname"];
                                XmlAttribute codefile = testNode.Attributes["file"];
                                XmlAttribute linenumber = testNode.Attributes["linenumber"];
                                if (nameAttribute != null && !String.IsNullOrWhiteSpace(nameAttribute.Value))
                                {
                                    string name;
                                    string displayName;

                                    if (classnameAttribute != null && !String.IsNullOrWhiteSpace(classnameAttribute.Value))
                                    {
                                        name = IglooSpecNameFormatter.buildTestName(classnameAttribute.Value, nameAttribute.Value);
                                        displayName = IglooSpecNameFormatter.buildDisplayName(classnameAttribute.Value, nameAttribute.Value);
                                    }
                                    else
                                    {
                                        name = IglooSpecNameFormatter.buildTestName(nameAttribute.Value);
                                        displayName = IglooSpecNameFormatter.buildDisplayName(nameAttribute.Value);
                                    }

                                    var testCase = new TestCase(name, SnowPlowTestExecutor.ExecutorUri, source);
                                    testCase.DisplayName = displayName;

                                    if (codefile != null && !String.IsNullOrWhiteSpace(codefile.Value))
                                    {
                                        testCase.CodeFilePath = codefile.Value;

                                        uint number;
                                        if (linenumber != null && uint.TryParse(linenumber.Value, out number))
                                        {
                                            testCase.LineNumber = (int)number;
                                        }
                                    }

                                    tests.Add(testCase);

                                    if (discoverySink != null)
                                    {
                                        discoverySink.SendTestCase(testCase);
                                    }
                                }
                            }
                        }

                        int timeout = 10000;
                        unittestProcess.WaitForExit(timeout);

                        if (!unittestProcess.HasExited)
                        {
                            unittestProcess.Kill();
                            logger.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Ran out of time plowing tests in {0}, test process has been killed.", source));
                            continue;
                        }

                        if (unittestProcess.ExitCode < 0)
                        {
                            logger.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Broke plow, {0} returned exit code {1}", source, unittestProcess.ExitCode));
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log error.
                    string message = string.Format("SnowPlow: Exception thrown through windscreen: {0}", e.Message);
                    Debug.Assert(false, message);
                    logger.SendMessage(TestMessageLevel.Error, message);
                }
            }

            return tests;
        }
    }
}
