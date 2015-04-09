using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

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

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            m_cancelled = false;

            //TODO: First check which files we need to run and only run each one once rather than for every test case.
            foreach (TestCase test in tests)
            {
                if (m_cancelled)
                {
                    break;
                }

                frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Plowing through {0}", test.FullyQualifiedName));

                // Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = test.Source;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "--output=xunit";

                var testResult = new TestResult(test);

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

                                // Check we are looking at the test we wanted to run
                                if (name == test.FullyQualifiedName)
                                {
                                    XmlNode failureNode = testNode.SelectSingleNode("failure");
                                    if (failureNode == null)
                                    {
                                        // Success, yay
                                        testResult.Outcome = TestOutcome.Passed;
                                    }
                                    else
                                    {
                                        XmlAttribute messageAttribute = failureNode.Attributes["message"];
                                        testResult.ErrorMessage = messageAttribute.Value;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log error.
                    frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Ran of the road. {0}", e.Message));
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = "SnowPlow: Exception when running/parsing unit test: " + e.Message;
                }

                frameworkHandle.RecordResult(testResult);
            }
        }

        public void Cancel()
        {
            m_cancelled = true;
        }
    }
}
