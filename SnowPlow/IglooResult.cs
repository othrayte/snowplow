using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace SnowPlow
{
    public class IglooResult
    {
        public IglooResult() { }

        public string ErrorMessage { get; set; }
        public string File { get; set; }
        public int LineNo { get; set; }
        public TestOutcome Outcome { get; set; }
    }
}
