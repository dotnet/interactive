// #if NETSTANDARD2_0

#nullable enable

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility;

internal class IOExtensions
{
    private const int DefaultBufferSize = 4096;
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    internal static Task<string> ReadAllTextAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
#if !NETSTANDARD2_0
            => File.ReadAllTextAsync(filePath, encoding, cancellationToken);
#else
            => ReadAllTextInternalAsync(filePath, encoding, cancellationToken);

    private static async Task<string> ReadAllTextInternalAsync(
        string filePath,
        Encoding? encoding,
        CancellationToken cancellationToken)
    {
        encoding ??= DefaultEncoding;

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
