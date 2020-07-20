/*
 * Copyright (C) 2020 Arian Dashti.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text;
using Khayyam.Util;

namespace Khayyam
{
    /// <summary>
    /// Implements soft line wrapping on an Appendable. To use, Append characters using <seealso cref="Append"/>
    /// or soft-wrapping spaces using <seealso cref="WrappingSpace"/>.
    /// </summary>
    public sealed class LineWrapper
    {
        private readonly RecordingAppendable _out;
        private readonly string _indent;
        private readonly int _columnLimit;
        private bool _closed;

        
        /// <summary>
        /// Characters written since the last wrapping space that haven't yet been flushed.
        /// </summary>
        private readonly AppendableStringBuilder _buffer = new AppendableStringBuilder();

        /// <summary>
        /// The number of characters since the most recent newline. Includes both out and the buffer. </summary>
        private int _column = 0;

        /// <summary>
        /// -1 if we have no buffering; otherwise the number of {@code indent}s to write after wrapping.
        /// </summary>
        private int _indentLevel = -1;

        /// <summary>
        /// Null if we have no buffering; otherwise the type to pass to the next call to <seealso cref="Flush"/>.
        /// </summary>
        private FlushType? _nextFlush;

        public LineWrapper(IAppendable @out, string indent, int columnLimit)
        {
            _out = new RecordingAppendable(@out);
            _indent = indent;
            _columnLimit = columnLimit;
        }

        /// <returns>
        /// the last emitted char or <seealso cref="char.MinValue"/> if nothing emitted yet.
        /// </returns>
        internal char LastChar()
        {
            return _out.LastChar;
        }

        /// <summary>
        /// Emit {@code s}. This may be buffered to permit line wraps to be inserted.
        /// </summary>
        public void Append(string s)
        {
            if (_closed)
            {
                throw new System.InvalidOperationException("closed");
            }

            if (_nextFlush != null)
            {
                var nextNewline = s.IndexOf('\n');

                // If s doesn't cause the current line to cross the limit, buffer it and return. We'll decide
                // whether or not we have to wrap it later.
                if (nextNewline == -1 && _column + s.Length <= _columnLimit)
                {
                    _buffer.Append(s);
                    _column += s.Length;
                    return;
                }

                // Wrap if Appending s would overflow the current line.
                var wrap = nextNewline == -1 || _column + nextNewline > _columnLimit;
                Flush(wrap ? FlushType.Wrap : _nextFlush);
            }

            _out.Append(s);
            var lastNewline = s.LastIndexOf('\n');
            _column = lastNewline != -1 ? s.Length - lastNewline - 1 : _column + s.Length;
        }

        /// <summary>
        /// Emit either a space or a newline character.
        /// </summary>
        public void WrappingSpace(int indentLevel)
        {
            if (_closed)
            {
                throw new System.InvalidOperationException("closed");
            }

            if (_nextFlush != null)
            {
                Flush(_nextFlush);
            }

            _column++; // Increment the column even though the space is deferred to next call to flush().
            _nextFlush = FlushType.Space;
            _indentLevel = indentLevel;
        }

        /// <summary>
        /// Emit a newline character if the line will exceed it's limit, otherwise do nothing.
        /// </summary>
        public void ZeroWidthSpace(int indentLevel)
        {
            if (_closed)
            {
                throw new System.InvalidOperationException("closed");
            }

            if (_column == 0)
            {
                return;
            }

            if (_nextFlush != null)
            {
                Flush(_nextFlush);
            }

            _nextFlush = FlushType.Empty;
            _indentLevel = indentLevel;
        }

        /// <summary>
        /// Flush any outstanding text and forbid future writes to this line wrapper. </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void close() throws java.io.IOException
        public void Close()
        {
            if (_nextFlush != null)
            {
                Flush(_nextFlush);
            }

            _closed = true;
        }

        /// <summary>
        /// Write the space followed by any buffered text that follows it.
        /// </summary>
        private void Flush(FlushType? flushType)
        {
            switch (flushType)
            {
                case FlushType.Wrap:
                    _out.Append('\n');
                    for (var i = 0; i < _indentLevel; i++)
                    {
                        _out.Append(_indent);
                    }

                    _column = _indentLevel * _indent.Length;
                    _column += _buffer.Length;
                    break;
                case FlushType.Space:
                    _out.Append(' ');
                    break;
                case FlushType.Empty:
                    break;
                default:
                    throw new System.ArgumentException("Unknown FlushType: " + flushType);
            }

            _out.Append(_buffer.ToString());
            _buffer.Remove(0, _buffer.Length);
            _indentLevel = -1;
            _nextFlush = null;
        }

        private enum FlushType
        {
            Wrap,
            Space,
            Empty
        }

        /// <summary>
        /// A delegating <seealso cref="IAppendable"/> that records info about the chars passing through it. </summary>
        private sealed class RecordingAppendable : IAppendable
        {
            private readonly IAppendable _delegate;

            internal char LastChar = char.MinValue;

            internal RecordingAppendable(IAppendable @delegate)
            {
                _delegate = @delegate;
            }

            public IAppendable Append(string str)
            {
                var length = str.Length;
                if (length != 0)
                {
                    LastChar = str[length - 1];
                }

                return _delegate.Append(str);
            }

            public IAppendable Append(string str, int start, int end)
            {
                var sub = str.Substring(start, end - start);
                return Append(sub);
            }

            public IAppendable Append(char c)
            {
                LastChar = c;
                return _delegate.Append(c);
            }
        }
    }
}