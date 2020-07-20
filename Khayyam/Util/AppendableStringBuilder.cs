using System.Text;

namespace Khayyam.Util
{
    public class AppendableStringBuilder : IAppendable
    {
        private readonly StringBuilder _builder;

        public AppendableStringBuilder()
        {
            _builder = new StringBuilder();
        }

        public int Length => _builder.Length;

        public AppendableStringBuilder Remove(int startIndex, int length)
        {
            _builder.Remove(startIndex, length);
            return this;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public IAppendable Append(string str)
        {
            _builder.Append(str);
            return this;
        }

        public IAppendable Append(string str, int start, int end)
        {
            var sub = str.Substring(start, end - start);
            _builder.Append(sub);
            return this;
        }

        public IAppendable Append(char c)
        {
            _builder.Append(c);
            return this;
        }
    }
}