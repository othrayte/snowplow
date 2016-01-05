using EnsureThat;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Globalization;

namespace SnowPlow
{
    public class Logger
    {
        IMessageLogger BaseLogger { get; set; }

        public Logger(IMessageLogger baseLogger)
        {
            BaseLogger = baseLogger;
        }

        public void Write(TestMessageLevel messageLevel, string format, params object[] args)
        {
            BaseLogger.SendMessage(messageLevel, strings.SnowPlow_ + string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public void WriteInformation(string format, params object[] args)
        {
            Write(TestMessageLevel.Informational, format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            Write(TestMessageLevel.Warning, format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            Write(TestMessageLevel.Error, format, args);
        }

        public void WriteException(Exception exception)
        {
            Ensure.That(() => exception).IsNotNull();
            Write(TestMessageLevel.Error, strings.ExceptionThrownMsg, exception.ToString());
        }
    }
}
