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
    public class IglooResult
    {
        public IglooResult() { }

        public string ErrorMessage { get; set; }
        public TestOutcome Outcome { get; set; }
    }

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

                frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("SnowPlow: Plowing through {0}", source));

                // Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = source;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "--output=xunit";

                try
                {
                    Dictionary<String, IglooResult> results = new Dictionary<string, IglooResult>();

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

                                IglooResult result = new IglooResult();
                                XmlNode failureNode = testNode.SelectSingleNode("failure");
                                if (failureNode == null)
                                {
                                    // Success, yay
                                    result.Outcome = TestOutcome.Passed;
                                }
                                else
                                {
                                    result.Outcome = TestOutcome.Failed;
                                    XmlAttribute messageAttribute = failureNode.Attributes["message"];
                                    result.ErrorMessage = messageAttribute.Value;
                                }
                                results[name] = result;
                            }
                        }
                    }
                    foreach (TestCase test in sources[source])
                    {
                        var testResult = new TestResult(test);
                        if (results.ContainsKey(test.FullyQualifiedName))
                        {
                            IglooResult result = results[test.FullyQualifiedName];
                            testResult.Outcome = result.Outcome;
                            testResult.ErrorMessage = result.ErrorMessage;
                        }
                        else
                        {
                            testResult.Outcome = TestOutcome.NotFound;
                        }
                        frameworkHandle.RecordResult(testResult);
                    }
                }
                catch (Exception e)
                {
                    // Log error.
                    frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format("SnowPlow: Ran of the road. {0}", e.Message));
                    foreach (TestCase test in sources[source])
                    {
                        var testResult = new TestResult(test);
                        testResult.Outcome = TestOutcome.Skipped;
                        testResult.ErrorMessage = "SnowPlow: Unable to perform unit test: " + e.Message;
                        frameworkHandle.RecordResult(testResult);
                    }
                }

            }
        }

        public void Cancel()
        {
            m_cancelled = true;
        }
    }
}
