using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Data;
using Microsoft.VisualStudio.TestWindow.Diagnostics;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;

namespace SnowPlow
{
    [Export(typeof(IGroupByProvider))]
    [Export]
    public sealed class GroupByFirstDescribe : IGroupByProvider<string>
    {
        private static readonly string noDescribeKey;
        private ILogger logger;
        private IUnitTestStorage unitTestStorage;

        static GroupByFirstDescribe()
        {
            noDescribeKey = string.Empty;
        }

        [ImportingConstructor]
        public GroupByFirstDescribe(IUnitTestStorage unitTestStorage, ILogger logger)
        {
            ValidateArg.NotNull<IUnitTestStorage>(unitTestStorage, "unitTestStorage");
            ValidateArg.NotNull<ILogger>(logger, "logger");
            this.unitTestStorage = unitTestStorage;
            this.logger = logger;
        }

        public static string ExtractDescribe(string spec)
        {
            return spec.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries).First();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.VisualStudio.TestWindow.Diagnostics.TimingLogger.#ctor(Microsoft.VisualStudio.TestWindow.Extensibility.ILogger,System.String,System.Boolean)")]
        public IEnumerable<object> Keys
        {
            get
            {
                IEnumerable<object> enumerable;
                using (new TimingLogger(this.logger, "GroupByFirstDescribe.Keys", false))
                {
                    using (ITestQueryable queryable = this.unitTestStorage.ActiveUnitTestReader.GetAllTests())
                    {
                        List<string> list = (from tokenizedDescribeName in
                                                 (from t in queryable.Cast<TestData>() select ExtractDescribe(t.FullyQualifiedName)).Distinct<string>().ToList<string>()
                                             where tokenizedDescribeName != noDescribeKey
                                             select tokenizedDescribeName).ToList<string>();
                        list.Add(noDescribeKey);
                        enumerable = list;
                    }
                }
                return enumerable;
            }

        }

        Expression IGroupByProvider.KeySelector
        {
            get
            {
                return this.KeySelector;
            }
        }

        public Type KeyType
        {
            get
            {
                return typeof(string);
            }
        }

        public Expression<Func<ITest, IEnumerable<string>>> KeySelector
        {
            get
            {
                return t => new string[] {
                    string.IsNullOrEmpty(((TestData) t).FullyQualifiedName)
                    ? noDescribeKey
                    : ExtractDescribe(((TestData) t).FullyQualifiedName)
                };
            }
        }

        KeyComparer IGroupByProvider.CompareKeys
        {
            get
            {
                return (x, y) => GroupByFirstDescribeComparer.Instance.Compare((string)x, (string)y);
            }
        }

        public string GetKeyDisplayName(object key)
        {
            string describe = (string)key;

            if (describe != noDescribeKey)
            {
                return describe;
            }
            return strings.UnknownDescribe;

        }

        public string DisplayName
        {
            get
            {
                return strings.RootDescribe;
            }
        }

        public IQueryable<ITest> FilterByKey(IQueryable<ITest> tests, object key)
        {
            string describe = (string)key;
            return (from t in tests
                    where (string.Compare(ExtractDescribe(((TestData)t).FullyQualifiedName), describe) == 0)
                    select t);

        }

        private class GroupByFirstDescribeComparer : IComparer<string>
        {
            private static IComparer<string> instance;

            private GroupByFirstDescribeComparer()
            {
            }

            public int Compare(string x, string y)
            {
                if ((x == GroupByFirstDescribe.noDescribeKey) && (y == GroupByFirstDescribe.noDescribeKey))
                {
                    return 0;
                }
                if (x == GroupByFirstDescribe.noDescribeKey)
                {
                    return 1;
                }
                if (y == GroupByFirstDescribe.noDescribeKey)
                {
                    return -1;
                }
                if (string.Equals(x, y))
                {
                    return 0;
                }
                return string.Compare(x, y, StringComparison.CurrentCulture);
            }

            public static IComparer<string> Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new GroupByFirstDescribe.GroupByFirstDescribeComparer();
                    }
                    return instance;
                }
            }
        }

    }
}
