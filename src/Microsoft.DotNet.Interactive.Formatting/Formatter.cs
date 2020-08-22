// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class Formatter
    {
        private static int _defaultListExpansionLimit;
        private static int _recursionLimit;
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        private static string _defaultMimeType = HtmlFormatter.MimeType;

        // user specification
        private static readonly ConcurrentStack<(Type type, string mimeType)> _preferredMimeTypes = new ConcurrentStack<(Type type, string mimeType)>();
        private static readonly ConcurrentStack<(Type type, string mimeType)> _defaultPreferredMimeTypes = new ConcurrentStack<(Type type, string mimeType)>();
        internal static readonly ConcurrentStack<ITypeFormatter> _typeFormatters = new ConcurrentStack<ITypeFormatter>();
        internal static readonly ConcurrentStack<ITypeFormatter> _defaultTypeFormatters = new ConcurrentStack<ITypeFormatter>();

        // computed state
        private static readonly ConcurrentDictionary<Type, string> _preferredMimeTypesTable = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<(Type type, string mimeType), ITypeFormatter> _typeFormattersTable = new ConcurrentDictionary<(Type type, string mimeType), ITypeFormatter>();
        private static readonly ConcurrentDictionary<Type, Action<FormatContext, object, TextWriter, string>> _genericFormattersTable = new ConcurrentDictionary<Type, Action<FormatContext, object, TextWriter, string>>();

        /// <summary>
        /// Initializes the <see cref="Formatter"/> class.
        /// </summary>
        static Formatter()
        {
            ResetToDefault();
        }

        internal static TextWriter CreateWriter() => new StringWriter(CultureInfo.InvariantCulture);

        internal static IPlainTextFormatter SingleLinePlainTextFormatter = new SingleLinePlainTextFormatter();

        /// <summary>
        /// Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence.
        /// </summary>
        /// <value>
        /// The list expansion limit.
        /// </value>
        public static int ListExpansionLimit
        {
            get => _defaultListExpansionLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"{nameof(ListExpansionLimit)} must be at least 0.");
                }

                _defaultListExpansionLimit = value;
            }
        }


        /// <summary>
        /// Gets or sets the string that will be written out for null items.
        /// </summary>
        /// <value>
        /// The null string.
        /// </value>
        public static string NullString { get; set; }

        /// <summary>
        /// Gets or sets the limit to how many levels the formatter will recurse into an object graph.
        /// </summary>
        /// <value>
        /// The recursion limit.
        /// </value>
        public static int RecursionLimit
        {
            get => _recursionLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"{nameof(RecursionLimit)} must be at least 0.");
                }

                _recursionLimit = value;
            }
        }

        internal static event EventHandler Clearing;

        /// <summary>
        /// Resets all formatters and formatter settings to their default values.
        /// </summary>
        public static void ResetToDefault()
        {
            ClearComputedState();
            _typeFormatters.Clear();
            _preferredMimeTypes.Clear();
            _defaultTypeFormatters.Clear();
            _defaultPreferredMimeTypes.Clear();
            
            // In the lists of default formatters, the highest priority ones come first,
            // so register those last.
            _defaultTypeFormatters.PushRange(HtmlFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(JsonFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(PlainTextFormatter.DefaultFormatters.Reverse().ToArray());

            // It is unclear if we need this default:
            _defaultPreferredMimeTypes.Push((typeof(string), PlainTextFormatter.MimeType));
            _defaultPreferredMimeTypes.Push((typeof(JToken), JsonFormatter.MimeType));

            ListExpansionLimit = 20;
            RecursionLimit = 6;
            NullString = "<null>";

            Clearing?.Invoke(null, EventArgs.Empty);
        }

        internal static void ClearComputedState()
        {
            _typeFormattersTable.Clear();
            _genericFormattersTable.Clear();
            _preferredMimeTypesTable.Clear();
        }

        public static void SetPreferredMimeTypeFor(Type type, string preferredMimeType, bool addToDefaults = false)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(preferredMimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(preferredMimeType));
            }

            ClearComputedState();
            if (addToDefaults)
            {
                _defaultPreferredMimeTypes.Push((type, preferredMimeType));
            }
            else
            {
                _preferredMimeTypes.Push((type, preferredMimeType));
            }
        }

        public static string DefaultMimeType
        {
            get => _defaultMimeType;
            set => _defaultMimeType = value ??
                                      throw new ArgumentNullException(nameof(value));
        }

        public static string PreferredMimeTypeFor(Type type) =>
            _preferredMimeTypesTable.GetOrAdd(type, InferMimeType);

        private class SortByRelevanceAndOrder<T> : IComparer<T>
        {
            private readonly Func<T, Type> _typeKey;
            private readonly Func<T, int> _indexKey;

            public SortByRelevanceAndOrder(Func<T, Type> typeKey, Func<T, int> indexKey)
            {
                _typeKey = typeKey;
                _indexKey = indexKey;
            }

            public int Compare(T inp1, T inp2)
            {
                var type1 = _typeKey(inp1);
                var type2 = _typeKey(inp2);
                var index1 = _indexKey(inp1);
                var index2 = _indexKey(inp2);

                if (type1.IsRelevantFormatterFor(type2) && type2.IsRelevantFormatterFor(type1))
                {
                    return Comparer<int>.Default.Compare(index1, index2);
                }
                else if (type1.IsRelevantFormatterFor(type2))
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        private static string TryInferMimeType(Type type, IEnumerable<(Type type, string mimeType)> mimeTypes)
        {
            // Find the most specific type that specifies a mimeType
            var candidates =
                mimeTypes
                    .Where(mimeSpec => mimeSpec.type.IsRelevantFormatterFor(type))
                    .Select((x, index) => (x.type, x.mimeType, index))
                    .ToArray();

            if (candidates.Length > 0)
            {
                Array.Sort(candidates, new SortByRelevanceAndOrder<(Type type, string mimeType, int index)>(tup => tup.type, tup => tup.index));
                return candidates[0].mimeType;
            }
            else
            {
                return null;
            }
        }

        private static string InferMimeType(Type type)
        {
            var mimeType = TryInferMimeType(type, _preferredMimeTypes);
            if (mimeType != null)
            {
                return mimeType;
            }

            mimeType = TryInferMimeType(type, _defaultPreferredMimeTypes);
            if (mimeType != null)
            {
                return mimeType;
            }

            return _defaultMimeType;
        }

        public static string ToDisplayString(
            this object obj,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (mimeType == null)
            {
                throw new ArgumentNullException(nameof(mimeType));
            }

            using var writer = CreateWriter();
            var context = new FormatContext();
            FormatTo(obj, context, writer, mimeType);
            return writer.ToString();
        }

        public static string ToDisplayString(
            this object obj,
            ITypeFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            using var writer = CreateWriter();
            var context = new FormatContext();
            formatter.Format(context, obj, writer);
            return writer.ToString();
        }

        /// <summary>Invoke the formatter, creating a new format context</summary>
        public static void FormatTo<T>(this T obj,
            TextWriter writer,
            string mimeType = PlainTextFormatter.MimeType)
        {
            obj.FormatTo(new FormatContext(), writer, mimeType);
        }

        /// <summary>Invoke the formatter</summary>
        public static void FormatTo<T>(
            this T obj,
            FormatContext context,
            TextWriter writer,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (obj != null)
            {
                var actualType = obj.GetType();

                if (typeof(T) != actualType)
                {
                    // in some cases the generic parameter is Object but the object is of a more specific type, in which case get or add a cached accessor to the more specific Formatter<T>.Format method
                    var genericFormatter = _genericFormattersTable.GetOrAdd(actualType, GetGenericFormatterMethod);
                    genericFormatter(context, obj, writer, mimeType);
                    return;
                }
            }

            Formatter<T>.FormatTo(context, obj, writer, mimeType);
        }

        internal static Action<FormatContext, object, TextWriter, string> GetGenericFormatterMethod(this Type type)
        {
            var methodInfo = typeof(Formatter<>)
                             .MakeGenericType(type)
                             .GetMethod(nameof(Formatter<object>.FormatTo), new[]
                             {
                                 typeof(FormatContext),
                                 type,
                                 typeof(TextWriter),
                                 typeof(string)
                             });

            var contextParam = Expression.Parameter(typeof(FormatContext), "context");
            var targetParam = Expression.Parameter(typeof(object), "target");
            var writerParam = Expression.Parameter(typeof(TextWriter), "writer");
            var mimeTypeParam = Expression.Parameter(typeof(string), "mimeType");

            var methodCallExpr = Expression.Call(null,
                                                 methodInfo,
                                                 contextParam,
                                                 Expression.Convert(targetParam, type),
                                                 writerParam,
                                                 mimeTypeParam);

            return Expression.Lambda<Action<FormatContext, object, TextWriter, string>>(
                methodCallExpr,
                contextParam,
                targetParam,
                writerParam,
                mimeTypeParam).Compile();
        }

        internal static void Join(
            FormatContext context, 
            IEnumerable list,
            TextWriter writer,
            int? listExpansionLimit = null) =>
            Join(context, list.Cast<object>(), writer, listExpansionLimit);

        internal static void Join<T>(
            FormatContext context, 
            IEnumerable<T> list,
            TextWriter writer,
            int? listExpansionLimit = null)
        {
            if (list == null)
            {
                writer.Write(NullString);
                return;
            }

            var i = 0;

            SingleLinePlainTextFormatter.WriteStartSequence(writer);

            listExpansionLimit ??= Formatter<T>.ListExpansionLimit;

            using (var enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (i < listExpansionLimit)
                    {
                        // write out another item in the list
                        if (i > 0)
                        {
                            SingleLinePlainTextFormatter.WriteSequenceDelimiter(writer);
                        }

                        i++;

                        SingleLinePlainTextFormatter.WriteStartSequenceItem(writer);

                        enumerator.Current.FormatTo(context, writer);
                    }
                    else
                    {
                        // write out just a count of the remaining items in the list
                        var difference = list.Count() - i;
                        if (difference > 0)
                        {
                            writer.Write(" ... (");
                            writer.Write(difference);
                            writer.Write(" more)");
                        }

                        break;
                    }
                }
            }

            SingleLinePlainTextFormatter.WriteEndSequence(writer);
        }

        public static IEnumerable<string> RegisteredMimeTypesFor(Type type)
        {
            return _typeFormatters.Concat(_defaultTypeFormatters).Where(k => k.Type == type).Select(k => k.MimeType);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting.
        /// </summary>
        public static void Register(ITypeFormatter formatter, bool addToDefaults = false)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            ClearComputedState();

            if (addToDefaults)
            {
                _defaultTypeFormatters.Push(formatter);
            }
            else
            {
                _typeFormatters.Push(formatter);
            }
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        public static void Register<T>(
            Func<FormatContext, T, TextWriter, bool> formatter,
            string mimeType = PlainTextFormatter.MimeType,
            bool addToDefaults = false)
        {
            Register(new AnonymousTypeFormatter<T>(formatter, mimeType), addToDefaults);
        }


        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <paramref name="type" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        /// <param name="type">The type the formatter is registered for.</param>
        public static void Register(
            Type type,
            Func<FormatContext, object, TextWriter, bool> formatter,
            string mimeType = PlainTextFormatter.MimeType, 
            bool addToDefaults = false)
        {
            Register(new AnonymousTypeFormatter<object>(formatter, mimeType, type), addToDefaults);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <paramref name="type" />.
        /// </summary>
        /// <param name="formatter">The formatting action.</param>
        /// <param name="type">The type the formatter is registered for.</param>
        public static void Register(
            Type type,
            Action<object, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType,
            bool addToDefaults = false)
        {
            Register(new AnonymousTypeFormatter<object>((context, value, writer) =>
            {
                formatter(value, writer); 
                return true;
            }, mimeType, type), addToDefaults);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatting action.</param>
        public static void Register<T>(
            Action<T, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType,
            bool addToDefaults = false)
        {
            Register(new AnonymousTypeFormatter<object>((context, value, writer) =>
            {
                formatter((T)value, writer); 
                return true;
            }, mimeType, typeof(T)), addToDefaults);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        public static void Register<T>(
            Func<T, string> formatter,
            string mimeType = PlainTextFormatter.MimeType,
            bool addToDefaults = false)
        {
            Register(new AnonymousTypeFormatter<T>((context, value, writer) =>
            {
                writer.Write(formatter(value)); 
                return true;
            }, mimeType), addToDefaults);
        }

        public static IEnumerable<ITypeFormatter> RegisteredFormatters(bool includeDefaults = true)
        {
            foreach (var formatter in _typeFormatters)
            {
                yield return formatter;
            }

            if (includeDefaults)
            {
                foreach (var formatter in _defaultTypeFormatters)
                {
                    yield return formatter;
                }
            }
        }

        public static ITypeFormatter GetPreferredFormatterFor(Type actualType, string mimeType = PlainTextFormatter.MimeType)
        {
            return
                _typeFormattersTable
                    .GetOrAdd(
                        (actualType, mimeType),
                        tuple => InferPreferredFormatter(actualType, mimeType));
        }

        internal static ITypeFormatter InferPreferredFormatter(Type actualType, string mimeType)
        {
            // Try to find a user-specified type formatter, use the most specific type with a matching mime type
            var userFormatter = TryInferPreferredFormatter(actualType, mimeType, _typeFormatters);
            if (userFormatter != null)
            {
                return userFormatter;
            }

            // Try to find a default built-in type formatter, use the most specific type with a matching mime type
            var defaultFormatter = TryInferPreferredFormatter(actualType, mimeType, _defaultTypeFormatters);
            if (defaultFormatter != null)
            {
                return defaultFormatter;
            }

            // Last resort backup 
            return new AnonymousTypeFormatter<object>((context, obj, writer) =>
            {
                writer.Write(obj);
                return true;
            }, mimeType, actualType);
        }

        internal static ITypeFormatter TryInferPreferredFormatter(Type actualType, string mimeType, IEnumerable<ITypeFormatter> formatters)
        {

            // Find the most specific type that specifies a mimeType
            var candidates =
                formatters
                    .Where(formatter => formatter.MimeType == mimeType && formatter.Type.IsRelevantFormatterFor(actualType))
                    .Select((x, i) => (formatter: x, index: i))
                    .ToArray();

            if (candidates.Length == 1)
               { return candidates[0].formatter;}

            if (candidates.Length > 0)
            {
                Array.Sort(candidates, new SortByRelevanceAndOrder<(ITypeFormatter formatter, int index)>(tup => tup.formatter.Type, tup => tup.index));

                // Compose the possible formatters into one formatter, trying each in turn
                return new AnonymousTypeFormatter<object>((context, obj, writer) =>
                    {
                        foreach (var formatter in candidates)
                        {
                            if (formatter.formatter.Format(context, obj, writer))
                               { return true;}
                        }
                      {  return false;}
                    }, mimeType);
            }

            // A last restorr
            return null;
        }

        private static IReadOnlyCollection<T> ReadOnlyMemoryToArray<T>(ReadOnlyMemory<T> mem) => mem.Span.ToArray();

        internal static readonly MethodInfo FormatReadOnlyMemoryMethod = 
            typeof(Formatter)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(ReadOnlyMemoryToArray));


    }

    internal class FormatterTable
    {
        ConcurrentDictionary<(Type type, bool flag), ITypeFormatter> _formatters = new ConcurrentDictionary<(Type type, bool flag), ITypeFormatter>();
        Type _genericDef;
        string _name;
        internal FormatterTable(Type genericDef, string name)
        {
            _genericDef = genericDef;
            _name = name;
        }
        internal ITypeFormatter GetFormatter(Type type, bool flag)
        {
            return
                _formatters.GetOrAdd((type, flag),
                    tup =>
                    {
                        return
                          (ITypeFormatter)
                            _genericDef
                            .MakeGenericType(tup.type)
                            .GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                            .Invoke(null, new object[] { flag });
                    });
        }

    }
}