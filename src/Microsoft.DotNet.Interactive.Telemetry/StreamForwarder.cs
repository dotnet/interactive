// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public sealed class StreamForwarder
    {
        private static readonly char[] s_ignoreCharacters = new char[] { '\r' };
        private static readonly char s_flushBuilderCharacter = '\n';

        private StringBuilder _builder;
        private StringWriter _capture;
        private Action<string> _writeLine;

        public string CapturedOutput => _capture?.GetStringBuilder()?.ToString();

        public StreamForwarder Capture()
        {
            ThrowIfCaptureSet();

            _capture = new StringWriter();

            return this;
        }

        public StreamForwarder ForwardTo(Action<string> writeLine)
        {
            ThrowIfNull(writeLine);

            ThrowIfForwarderSet();

            _writeLine = writeLine;

            return this;
        }

        public Task BeginRead(TextReader reader)
        {
            return Task.Run(() => Read(reader));
        }

        public void Read(TextReader reader)
        {
            const int bufferSize = 1;

            var buffer = new char[bufferSize];
            _builder = new StringBuilder();

            // Using Read with buffer size 1 to prevent looping endlessly
            // like we would when using Read() with no buffer
            while ((reader.Read(buffer, 0, bufferSize)) > 0)
            {
                var currentCharacter = buffer[0];

                if (currentCharacter == s_flushBuilderCharacter)
                {
                    WriteBuilder();
                }
                else if (!s_ignoreCharacters.Contains(currentCharacter))
                {
                    _builder.Append(currentCharacter);
                }
            }

            // Flush anything else when the stream is closed
            // Which should only happen if someone used console.Write
            WriteBuilder();
        }

        private void WriteBuilder()
        {
            if (_builder.Length == 0)
            {
                return;
            }

            WriteLine(_builder.ToString());
            _builder.Clear();
        }

        private void WriteLine(string str)
        {
            _capture?.WriteLine(str);

            _writeLine?.Invoke(str);
        }

        private void ThrowIfNull(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }

        private void ThrowIfForwarderSet()
        {
            if (_writeLine != null)
            {
                throw new InvalidOperationException("WriteLine forwarder set previously"); // TODO: Localize this?
            }
        }

        private void ThrowIfCaptureSet()
        {
            if (_capture != null)
            {
                throw new InvalidOperationException("Already capturing stream!"); // TODO: Localize this?
            }
        }
    }
}
