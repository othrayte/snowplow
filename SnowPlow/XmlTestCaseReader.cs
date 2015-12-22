using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SnowPlow
{
    public class XmlTestCaseReader
    {
        private ITestCaseDiscoverySink testCaseSink;
        public List<TestCase> TestCases { get; private set; }

        public XmlTestCaseReader(ITestCaseDiscoverySink testCaseSink)
        {
            this.testCaseSink = testCaseSink;
            TestCases = new List<TestCase>();
        }

        public void read(string testSource, string content)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

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

                    var testCase = new TestCase(name, SnowPlowTestExecutor.ExecutorUri, testSource);
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

                    TestCases.Add(testCase);
                    if (testCaseSink != null) // The sink will be null when test discovery is called as part of test execution.
                    {
                        testCaseSink.SendTestCase(testCase);
                    }
                }
            }
        }

    }
}
