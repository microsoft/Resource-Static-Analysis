/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;

namespace Microsoft.CustomTraceListeners
{
    /// <summary>
    /// ConsoleTraceListner with custom output formatting: removes trace source information and replaces it with a 
    /// TimeSpan.Elapsed time from the begining of the execution.
    /// </summary>
    public class CustomFormatConsoleTraceListener : ConsoleTraceListener
    {
        private readonly Stopwatch _sw = new Stopwatch();

        public CustomFormatConsoleTraceListener()
            : base()
        {
            _sw.Start();
        }

        public CustomFormatConsoleTraceListener(bool useErrorStream)
            : base(useErrorStream)
        {
            _sw.Start();
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message)
        {
            WriteLine(eventType.ToString() + ": " + message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            WriteLine(String.Format(CultureInfo.CurrentCulture, eventType.ToString() + ": " + format, args));
        }

        public override void Write(string message)
        {
            base.Write(TraceListnerMessageFormatter.AddTime(message, _sw.Elapsed));
        }

        public override void WriteLine(string message)
        {
            base.WriteLine(TraceListnerMessageFormatter.AddTime(message, _sw.Elapsed));
        }
    }

    /// <summary>
    /// TextWriterTraceListener with custom output formatting: removes trace source information and replaces it with a 
    /// TimeSpan.Elapsed time from the begining of the execution.
    /// </summary>
    public class CustomFormatTextWriterTraceListener : TextWriterTraceListener
    {
        private readonly Stopwatch sw = new Stopwatch();

        #region Constructors

        public CustomFormatTextWriterTraceListener()
            : base()
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class, using the stream as the recipient of the debugging and tracing output.
        //
        // Parameters:
        //   stream:
        //     A System.IO.Stream that represents the stream the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     writes to.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The stream is null.
        public CustomFormatTextWriterTraceListener(Stream stream)
            : base(stream)
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class, using the file as the recipient of the debugging and tracing output.
        //
        // Parameters:
        //   fileName:
        //     The name of the file the System.Diagnostics.CustomFormatTextWriterTraceListener writes
        //     to.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The file is null.
        public CustomFormatTextWriterTraceListener(string fileName)
            : base(fileName)
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class using the specified writer as recipient of the tracing or debugging
        //     output.
        //
        // Parameters:
        //   writer:
        //     A System.IO.TextWriter that receives the output from the System.Diagnostics.CustomFormatTextWriterTraceListener.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The writer is null.
        public CustomFormatTextWriterTraceListener(TextWriter writer)
            : base(writer)
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class with the specified name, using the stream as the recipient of the debugging
        //     and tracing output.
        //
        // Parameters:
        //   stream:
        //     A System.IO.Stream that represents the stream the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     writes to.
        //
        //   name:
        //     The name of the new instance.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The stream is null.
        public CustomFormatTextWriterTraceListener(Stream stream, string name)
            : base(stream, name)
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class with the specified name, using the file as the recipient of the debugging
        //     and tracing output.
        //
        // Parameters:
        //   fileName:
        //     The name of the file the System.Diagnostics.CustomFormatTextWriterTraceListener writes
        //     to.
        //
        //   name:
        //     The name of the new instance.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The stream is null.
        public CustomFormatTextWriterTraceListener(string fileName, string name)
            : base(fileName, name)
        {
            sw.Start();
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CustomFormatTextWriterTraceListener
        //     class with the specified name, using the specified writer as recipient of
        //     the tracing or debugging output.
        //
        // Parameters:
        //   writer:
        //     A System.IO.TextWriter that receives the output from the System.Diagnostics.CustomFormatTextWriterTraceListener.
        //
        //   name:
        //     The name of the new instance.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The writer is null.
        public CustomFormatTextWriterTraceListener(TextWriter writer, string name)
            : base(writer, name)
        {
            sw.Start();
        }

        #endregion

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message)
        {
            WriteLine(eventType.ToString() + ": " + message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            WriteLine(String.Format(CultureInfo.CurrentCulture, eventType.ToString() + ": " + format, args));
        }

        public override void Write(string message)
        {
            base.Write(TraceListnerMessageFormatter.AddTime(message, sw.Elapsed));
        }

        public override void WriteLine(string message)
        {
            base.WriteLine(TraceListnerMessageFormatter.AddTime(message, sw.Elapsed));
        }
    }

    internal static class TraceListnerMessageFormatter
    {
        public static string AddTime(string message, TimeSpan time)
        {
            return String.Format(CultureInfo.CurrentCulture, "[{0:D2}:{1:D2}:{2:D2}] {3}", (long) time.TotalHours,
                time.Minutes, time.Seconds, message);
        }
    }
}
