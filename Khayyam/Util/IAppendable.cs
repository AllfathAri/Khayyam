namespace Khayyam.Util
{
    public interface IAppendable
    {
        IAppendable Append(string str);
        
        IAppendable Append(string str, int start, int end);

        IAppendable Append(char c);
    }
}