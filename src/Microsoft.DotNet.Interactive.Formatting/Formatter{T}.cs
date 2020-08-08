// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    /// Provides formatting functionality for a specific type.
    /// </summary>
    /// <typeparam name="T">The type for which formatting is provided.</typeparam>
    public static class Formatter<T>
    {
        internal static readonly bool TypeIsAnonymous = typeof(T).IsAnonymous();
        internal static readonly bool TypeIsException = typeof(Exception).IsAssignableFrom(typeof(T));
        internal static readonly bool TypeIsTuple = typeof(T).IsTuple();
        internal static readonly bool TypeIsValueTuple = typeof(T).IsValueTuple();

        private static int? _listExpansionLimit;

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            void Initialize()
            {
                _listExpansionLimit = null;
            }

            Initialize();

            Formatter.Clearing += (o, e) => Initialize();
        }

        public static void Register(
            Action<T, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Formatter.Register(new AnonymousTypeFormatter<T>(formatter, mimeType));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        public static void Register(
            Func<T, string> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Formatter.Register(new AnonymousTypeFormatter<T>((obj, writer) => writer.Write(formatter((T)obj)), mimeType));
        }

        /// <summary>
        /// Formats an object and writes it to the specified writer.
        /// </summary>
        /// <param name="obj">The object to be formatted.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="mimeType">The mime type to format to.</param>
        public static void FormatTo(
            T obj,
            TextWriter writer,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (obj == null)
            {
                var formatter = Formatter.GetBestFormatter(typeof(T), mimeType);
                formatter.Format(null, writer);
                return;
            }

            using var _ = Formatter.RecursionCounter.Enter();

            // find a formatter for the object type, and possibly register one on the fly
            if (Formatter.RecursionCounter.Depth <= Formatter.RecursionLimit)
            {
                var formatter = Formatter.GetBestFormatter(typeof(T), mimeType);
                formatter.Format(obj, writer);
            }
            else
            {
                PlainTextFormatter<T>.Default.Format(obj, writer);
            }
        }

        public static int ListExpansionLimit
        {
            get => _listExpansionLimit ?? Formatter.ListExpansionLimit;
            set => _listExpansionLimit = value;
        }
    }
}