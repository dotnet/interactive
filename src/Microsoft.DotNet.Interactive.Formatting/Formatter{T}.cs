// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting;

/// <summary>
/// Provides formatting functionality for a specific type.
/// </summary>
/// <typeparam name="T">The type for which formatting is provided.</typeparam>
public static class Formatter<T>
{
    internal static readonly bool TypeIsAnonymous;
    internal static readonly bool TypeIsTuple;
    internal static readonly bool TypeIsValueTuple;
    internal static readonly bool TypeIsTupleOfScalars;
    internal static readonly bool TypeIsScalar;

    private static int? _listExpansionLimit;

    /// <summary>
    /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
    /// </summary>
    static Formatter()
    {
        if (TypeIsScalar = typeof(T).IsScalar())
        {
        }
        else if (TypeIsTuple = typeof(T).IsTuple())
        {
            var fieldTypes = typeof(T).GetProperties().Select(p => p.PropertyType);

            TypeIsTupleOfScalars = fieldTypes.All(t => t.IsScalar());
        }
        else if (TypeIsValueTuple = typeof(T).IsValueTuple())
        {
            var fieldTypes = typeof(T).GetFields().Select(f => f.FieldType);

            TypeIsTupleOfScalars = fieldTypes.All(t => t.IsScalar());
        }
        else
        {
            TypeIsAnonymous = typeof(T).IsAnonymous();
        }

        void Initialize()
        {
            _listExpansionLimit = null;
        }

        Initialize();

        Formatter.Clearing += Initialize;
    }

    /// <summary>
    /// Formats an object and writes it to the specified writer.
    /// </summary>
    /// <param name="obj">The object to be formatted.</param>
    /// <param name="context">The context for the current format operation.</param>
    /// <param name="mimeType">The mime type to format to.</param>
    public static void FormatTo(
        T obj,
        FormatContext context,
        string mimeType = PlainTextFormatter.MimeType)
    {
        if (obj is null)
        {
            var formatter = Formatter.GetPreferredFormatterFor(typeof(T), mimeType);
            formatter.Format(null, context);
            return;
        }

        using var _ = context.IncrementDepth();

        if (context.Depth <= Formatter.RecursionLimit)
        {
            var formatter = Formatter.GetPreferredFormatterFor(typeof(T), mimeType);
            formatter.Format(obj, context);
        }
        else
        {
            PlainTextFormatter<T>.Default.Format(obj, context);
        }
    }

    public static int ListExpansionLimit
    {
        get => _listExpansionLimit ?? Formatter.ListExpansionLimit;
        set => _listExpansionLimit = value;
    }
}