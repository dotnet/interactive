namespace Microsoft.DotNet.Interactive.HttpRequest
{
    public interface ITextSnapshotLine
    {
        int Start { get; }
        string GetTextIncludingLineBreak();
    }
}