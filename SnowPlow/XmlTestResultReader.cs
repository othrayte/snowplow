using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace SnowPlow
{
    class XmlTestResultReader
    {
        private ITestExecutionRecorder testResultSink;

        public XmlTestResultReader(ITestExecutionRecorder testResultSink)
        {
            this.testResultSink = testResultSink;
        }

        public void read(IEnumerable<TestCase> testCases, StreamReader streamReader)
        {
            try
            {
                Dictionary<String, IglooResult> results = new Dictionary<string, IglooResult>();

                using (XmlReader reader = XmlReader.Create(streamReader))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);

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
                                string message = messageAttribute.Value;

                                // Need single line option to match the multiline error message
                                Regex r = new Regex(@"([^(]+)[ ]?\(([0-9]+)\): (.*)$", RegexOptions.Singleline);
                                Match m = r.Match(message);
                                if (m.Success)
                                {
                                    result.File = m.Groups[1].Value;
                                    result.LineNo = Convert.ToInt32(m.Groups[2].Value);
                                    result.ErrorMessage = m.Groups[3].Value;
                                }
                                else
                                {
                                    result.ErrorMessage = message;
                                }
                            }
                            results[name] = result;
                        }
                    }
                }

                foreach (TestCase test in testCases)
                {
                    var testResult = new TestResult(test);
                    if (results.ContainsKey(test.FullyQualifiedName))
                    {
                        IglooResult result = results[test.FullyQualifiedName];
                        testResult.Outcome = result.Outcome;
                        testResult.ErrorStackTrace = result.File + ":" + result.LineNo;
                        testResult.ErrorMessage = result.ErrorMessage;
                    }
                    else
                    {
                        testResult.Outcome = TestOutcome.NotFound;
                    }
                    testResultSink.RecordResult(testResult);
                }

            }
            catch (Exception e)
            {
                foreach (TestCase test in testCases)
                {
                    var testResult = new TestResult(test);
                    testResult.Outcome = TestOutcome.Skipped;
                    testResult.ErrorMessage = "SnowPlow: Unable to perform unit test: " + e.Message;
                    testResultSink.RecordResult(testResult);
                }
                throw;
            }

        }
    }


}
