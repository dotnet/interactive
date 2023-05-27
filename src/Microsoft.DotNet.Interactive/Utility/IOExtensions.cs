// #if NETSTANDARD2_0

#nullable enable

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility;

internal class IOExtensions
{
    /// NOTE: This matches the default encoding used in <see cref="File.ReadAllTextAsync(string, CancellationToken)"/>.
    /// We don't expect this default to change as it would be a significant breaking change for .NET.
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    internal static Task<string> ReadAllTextAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        encoding ??= DefaultEncoding;

#if NETSTANDARD2_0
        return ReadAllTextInternalAsync(filePath, encoding, cancellationToken);
#else
        return File.ReadAllTextAsync(filePath, encoding, cancellationToken);
#endif
    }

#if NETSTANDARD2_0
    /// NOTE: This matches the default buffer size used in <see cref="File.ReadAllTextAsync"/>.
    private const int DefaultBufferSize = 4096;

    private static async Task<string> ReadAllTextInternalAsync(
        string filePath,
        Encoding encoding,
        CancellationToken cancellationToken)
    {
        using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                DefaultBufferSize,
                useAsync: true);

        var builder = new StringBuilder();
        var buffer = new byte[DefaultBufferSize];
        int bytesReadCount;

        while ((bytesReadCount = await stream.ReadAsync(buffer, offset: 0, buffer.Length, cancellationToken)) != 0)
        {
            var text = encoding.GetString(buffer, index: 0, bytesReadCount);
            builder.Append(text);
        }

        return builder.ToString();
    }
#endif
}
