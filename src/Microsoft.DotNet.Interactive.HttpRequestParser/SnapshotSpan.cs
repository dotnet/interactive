namespace Microsoft.DotNet.Interactive.HttpRequest;

internal record SnapshotSpan(int Start, int Length)
{
    public int End => Start + Length - 1;

    public bool IntersectsWith(SnapshotSpan span) =>
        span.Start <= End && span.End >= Start;
}
