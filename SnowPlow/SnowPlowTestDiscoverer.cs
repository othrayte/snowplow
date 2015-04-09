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
            GetTests(sources, logger, discoverySink);
            logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Finished looking for snow"));
        }

        internal static IEnumerable<TestCase> GetTests(IEnumerable<string> sources, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {

            List<TestCase> tests = new List<TestCase>();

            foreach (string source in sources)
            {
                if (!source.EndsWith("Tests.exe")) continue;

                logger.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Looking in {0}", source));

                // Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = source;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "--list --output=xunit";

                try
                {
                    // Start the process, Call WaitForExit and then the using statement will close.
                    using (Process unittestProcess = Process.Start(startInfo))
                    {
                        string output = "";
                        using (StreamReader reader = unittestProcess.StandardOutput)
                        {
                            output = reader.ReadToEnd();
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
                    logger.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Ran of the road. {0}", e.Message));
                }
            }

            return tests;
        }
    }
}
