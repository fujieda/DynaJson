using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DynaJson
{
    internal class JsonParser
    {
        public const int StringInitialCapacity = 32;
        public const int ReaderBufferSize = 512;

        private static readonly BufferPool<Buffer> BufferPool = new BufferPool<Buffer>();
        private Buffer _buffer;
        private TextReader _reader;
        private char[] _readBuffer;
        private int _available;
        private int _bufferIndex;
        private char _nextChar;
        private int _position;
        private bool _isEnd;
        private StringBuffer _stringBuffer;
        private Stack<Context> _stack;
        private static readonly bool[] WhiteSpace = new bool[' ' + 1];

        private class Buffer
        {
            public readonly char[] Read = new char[ReaderBufferSize];
            public readonly StringBuffer String = new StringBuffer();
            public readonly Stack<Context> Stack = new Stack<Context>();
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Context
        {
            [FieldOffset(0)] public JsonArray Array;
            [FieldOffset(0)] public JsonDictionary Dictionary;
            [FieldOffset(8)] public string Key;
        }

        static JsonParser()
        {
            WhiteSpace['\r'] = WhiteSpace['\n'] = WhiteSpace['\t'] = WhiteSpace[' '] = true;
            BufferPool.Return(new Buffer());
        }

        private void Setup(TextReader reader)
        {
            _buffer = BufferPool.Rent() ?? new Buffer();
            _readBuffer = _buffer.Read;
            _stringBuffer = _buffer.String;
            _stack = _buffer.Stack;
            _readBuffer[0] = '\0';
            _reader = reader;
            _available = _reader.ReadBlock(_readBuffer, 0, _readBuffer.Length);
            _isEnd = _available == 0;
            _nextChar = _readBuffer[0];
        }

        public static object Parse(TextReader reader, int maxDepth)
        {
            return new JsonParser().ParseInternal(reader, maxDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object ParseInternal(TextReader reader, int maxDepth)
        {
            Setup(reader);
            var context = new Context();
            var charBuffer = stackalloc char[StringInitialCapacity];

            SkipWhiteSpaces();
            while (true)
            {
                var value = new InternalObject();
                switch (_nextChar)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        value.Number = GetNumber();
                        break;
                    case 'n':
                        CheckToken("ull");
                        value.Type = JsonType.Null;
                        break;
                    case 't':
                        CheckToken("rue");
                        value.Type = JsonType.True;
                        break;
                    case 'f':
                        // ReSharper disable once StringLiteralTypo
                        CheckToken("alse");
                        value.Type = JsonType.False;
                        break;
                    case '"':
                        value.Type = JsonType.String;
                        value.String = GetString(charBuffer);
                        break;
                    case '[':
                        if (_stack.Count == maxDepth)
                            throw JsonParserException.TooDeepNesting(_stack.Count, _position);
                        Consume();
                        _stack.Push(context);
                        context = new Context
                        {
                            Array = new JsonArray()
                        };
                        SkipWhiteSpaces();
                        continue;
                    case ']':
                        if (context.Array == null)
                            throw JsonParserException.UnexpectedError(_nextChar, _position);
                        Consume();
                        value.Type = JsonType.Array;
                        value.Array = context.Array;
                        context = _stack.Pop();
                        break;
                    case '{':
                        if (_stack.Count == maxDepth)
                            throw JsonParserException.TooDeepNesting(_stack.Count, _position);
                        Consume();
                        _stack.Push(context);
                        context = new Context
                        {
                            Dictionary = new JsonDictionary()
                        };
                        goto GetKey;
                    case '}':
                        if (context.Dictionary == null)
                            throw JsonParserException.UnexpectedError(_nextChar, _position);
                        Consume();
                        value.Type = JsonType.Object;
                        value.Dictionary = context.Dictionary;
                        context = _stack.Pop();
                        break;
                    default:
                        if (_isEnd)
                            throw JsonParserException.UnexpectedEnd(_position);
                        throw JsonParserException.UnexpectedError(_nextChar, _position);
                }

                SkipWhiteSpaces();
                // Start
                if (_stack.Count == 0)
                {
                    // The buffer intentionally leaks in exceptional cases to simplify the code for exceptions.
                    BufferPool.Return(_buffer);
                    if (_isEnd)
                        return JsonObject.ToValue(value);
                    throw JsonParserException.UnexpectedError(_nextChar, _position);
                }
                // Array
                if (context.Key == null)
                {
                    context.Array.Add(value);
                    if (_nextChar == ']')
                        continue;
                    if (_nextChar != ',')
                        throw JsonParserException.ExpectingError("',' or ']'", _position);
                    Consume();
                    SkipWhiteSpaces();
                    continue;
                }
                // Object
                context.Dictionary.Add(context.Key, value);
                if (_nextChar == '}')
                    continue;
                if (_nextChar != ',')
                    throw JsonParserException.ExpectingError("',' or '}'", _position);
                Consume();

                GetKey:
                SkipWhiteSpaces();
                if (_nextChar == '}')
                    continue;
                if (_nextChar != '"')
                    throw JsonParserException.ExpectingError("string", _position);
                context.Key = GetString(charBuffer);
                SkipWhiteSpaces();
                if (_nextChar != ':')
                    throw JsonParserException.ExpectingError("':'", _position);
                Consume();
                SkipWhiteSpaces();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhiteSpaces()
        {
            while (true)
            {
                var ch = _nextChar;
                if (ch > ' ' || !WhiteSpace[ch])
                    return;
                Consume();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consume()
        {
            _bufferIndex++;
            _position++;
            if (_available == _bufferIndex)
            {
                _bufferIndex = 0;
                _available = _reader.ReadBlock(_readBuffer, 0, _readBuffer.Length);
                if (_available == 0)
                {
                    _isEnd = true;
                    _nextChar = '\0';
                    return;
                }
            }
            _nextChar = _readBuffer[_bufferIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckToken(string s)
        {
            Consume();
            foreach (var ch in s)
            {
                if (ch != _nextChar)
                    throw JsonParserException.ExpectingError($"'{ch}'", _position);
                Consume();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetNumber()
        {
            var result = 0d;
            var sign = 1;
            if (_nextChar == '-')
            {
                sign = -1;
                Consume();
                if (!IsNumber())
                    throw JsonParserException.ExpectingError("digit", _position);
            }
            do
            {
                result = result * 10.0 + (_nextChar - '0');
                Consume();
            } while (IsNumber());
            if (_nextChar == '.')
            {
                Consume();
                if (!IsNumber())
                    throw JsonParserException.ExpectingError("digit", _position);
                var exp = 0.1;
                do
                {
                    result += (_nextChar - '0') * exp;
                    exp *= 0.1;
                    Consume();
                } while (IsNumber());
            }
            if (_nextChar == 'e' || _nextChar == 'E')
            {
                Consume();
                var expSign = 1;
                var exp = 0;
                if (_nextChar == '-' || _nextChar == '+')
                {
                    if (_nextChar == '-')
                        expSign = -1;
                    Consume();
                }
                if (!IsNumber())
                    throw JsonParserException.ExpectingError("digit", _position);
                do
                {
                    exp = exp * 10 + (_nextChar - '0');
                    Consume();
                } while (IsNumber());
                result = result * Math.Pow(10, expSign * exp);
            }
            return sign * result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNumber()
        {
            return '0' <= _nextChar && _nextChar <= '9';
        }

        private unsafe string GetString(char* charBuffer)
        {
            Consume();
            var len = 0;
            while (true)
            {
                if (_isEnd)
                    throw JsonParserException.UnexpectedEnd(_position);
                var ch = _nextChar;
                if (ch == '"')
                {
                    Consume();
                    return _stringBuffer.GetString(charBuffer, len);
                }
                if (ch == '\\')
                {
                    ch = UnEscape();
                }
                else if (ch < ' ')
                {
                    throw JsonParserException.UnexpectedError(ch, _position);
                }
                if (len >= StringInitialCapacity)
                {
                    _stringBuffer.Append(charBuffer, len);
                    len = 0;
                }
                charBuffer[len++] = ch;
                Consume();
            }
        }

        private unsafe class StringBuffer
        {
            private char[] _buffer = new char[0];
            private int _position;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public string GetString(char* charBuffer, int len)
            {
                if (_position == 0)
                    return new string(charBuffer, 0, len);
                Append(charBuffer, len);
                var s = new string(_buffer, 0, _position);
                _position = 0;
                return s;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Append(char* charBuffer, int len)
            {
                if (_buffer.Length < _position + len)
                    Array.Resize(ref _buffer, _position + len);
                for (var i = 0; i < len; i++)
                    _buffer[_position + i] = charBuffer[i];
                _position += len;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char UnEscape()
        {
            Consume();
            var ch = _nextChar;
            switch (ch)
            {
                case '\\':
                case '/':
                case '"':
                    break;
                case 'b':
                    ch = '\b';
                    break;
                case 'f':
                    ch = '\f';
                    break;
                case 'n':
                    ch = '\n';
                    break;
                case 'r':
                    ch = '\r';
                    break;
                case 't':
                    ch = '\t';
                    break;
                case 'u':
                    ch = UnEscapeUnicode();
                    break;
                default:
                    throw JsonParserException.InvalidError($"escape character '{ch}'", _position);
            }
            return ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char UnEscapeUnicode()
        {
            var code = 0;
            for (var i = 0; i < 4; i++)
            {
                Consume();
                var ch = _nextChar;
                code <<= 4;
                if ('0' <= ch && ch <= '9')
                {
                    code += ch - '0';
                }
                else if ('a' <= ch && ch <= 'f')
                {
                    code += 10 + ch - 'a';
                }
                else if ('A' <= ch && ch <= 'F')
                {
                    code += 10 + ch - 'A';
                }
                else
                {
                    throw JsonParserException.InvalidError($"unicode escape '{ch}'", _position);
                }
            }
            return (char)code;
        }
    }

    public class JsonParserException : Exception
    {
        private JsonParserException(string message, string item, int position) :
            base($"{message} {item} at {position}")
        {
        }

        public static JsonParserException ExpectingError(string expecting, int position)
        {
            return new JsonParserException("Expecting", expecting, position);
        }

        public static JsonParserException UnexpectedError(char ch, int position)
        {
            return new JsonParserException("Unexpected", $"character '{ch}'", position);
        }

        public static JsonParserException InvalidError(string item, int position)
        {
            return new JsonParserException("Invalid", item, position);
        }

        public static JsonParserException UnexpectedEnd(int position)
        {
            return new JsonParserException("Unexpected", "end", position);
        }

        public static JsonParserException TooDeepNesting(int depth, int position)
        {
            return new JsonParserException("Too deep nesting", (depth + 1).ToString(), position);
        }
    }
}