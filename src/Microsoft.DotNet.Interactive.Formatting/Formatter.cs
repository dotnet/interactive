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
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class Formatter
    {
        private static int _defaultListExpansionLimit;
        private static int _recursionLimit;

        private static string _defaultMimeType;

        // user specification
        private static readonly ConcurrentStack<(Type type, HashSet<string> mimeTypes)> _preferredMimeTypes = new();
        private static readonly ConcurrentStack<(Type type, string mimeType)> _defaultPreferredMimeTypes = new();
        internal static readonly ConcurrentStack<ITypeFormatter> _userTypeFormatters = new();
        internal static readonly ConcurrentStack<ITypeFormatter> _defaultTypeFormatters = new();
        internal static readonly ConcurrentDictionary<Type, bool> _typesThatHaveBeenCheckedForFormatters = new();

        // computed state
        private static readonly ConcurrentDictionary<Type, HashSet<string>> _preferredMimeTypesTable = new();
        private static readonly ConcurrentDictionary<(Type type, string mimeType), ITypeFormatter> _typeFormattersTable = new();
        private static readonly ConcurrentDictionary<Type, Action<FormatContext, object, string>> _genericFormattersTable = new();

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

        internal static event Action Clearing;

        /// <summary>
        /// Resets all formatters and formatter settings to their default values.
        /// </summary>
        public static void ResetToDefault()
        {
            DefaultMimeType = HtmlFormatter.MimeType;
            ClearComputedState();

            _typesThatHaveBeenCheckedForFormatters.Clear();
            _userTypeFormatters.Clear();
            _preferredMimeTypes.Clear();
            _defaultTypeFormatters.Clear();
            _defaultPreferredMimeTypes.Clear();

            // In the lists of default formatters, the highest priority ones come first,
            // so register those last.

            // TODO: (ResetToDefault) remove the need to reverse these
            _defaultTypeFormatters.PushRange(TabularDataResourceFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(CsvFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(HtmlFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(JsonFormatter.DefaultFormatters.Reverse().ToArray());
            _defaultTypeFormatters.PushRange(PlainTextFormatter.DefaultFormatters.Reverse().ToArray());

            // It is unclear if we need this default:
            _defaultPreferredMimeTypes.Push((typeof(string), PlainTextFormatter.MimeType));

            ListExpansionLimit = 20;
            RecursionLimit = 6;
            NullString = "<null>";

            Clearing?.Invoke();
        }

        internal static void ClearComputedState()
        {
            _typeFormattersTable.Clear();
            _genericFormattersTable.Clear();
            _preferredMimeTypesTable.Clear();
        }

        public static void SetPreferredMimeTypesFor(Type type, params string[] preferredMimeTypes)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (preferredMimeTypes is null
                || preferredMimeTypes.Count( mimeType =>  !string.IsNullOrWhiteSpace(mimeType)) ==0)
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(preferredMimeTypes));
            }

            ClearComputedState();
            
            _preferredMimeTypes.Push((type, new HashSet<string>(preferredMimeTypes)));
        }

        public static string DefaultMimeType
        {
            get => _defaultMimeType;
            set => _defaultMimeType = value ??
                                      throw new ArgumentNullException(nameof(value));
        }

        public static IReadOnlyCollection<string> GetPreferredMimeTypesFor(Type type) =>
            _preferredMimeTypesTable.GetOrAdd(type ??= typeof(object), InferMimeTypes);

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

                var isType1RelevantForType2 = type1.IsRelevantFormatterFor(type2);

                if (isType1RelevantForType2 && type2.IsRelevantFormatterFor(type1))
                {
                    var index1 = _indexKey(inp1);
                    var index2 = _indexKey(inp2);
                    return Comparer<int>.Default.Compare(index1, index2);
                }
                else if (isType1RelevantForType2)
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

        private static HashSet<string> TryInferMimeTypes(Type type, IEnumerable<(Type type, HashSet<string> mimeTypes)> mimeTypes)
        {
            // Find the most specific type that specifies a mimeType
            var candidates =
                mimeTypes
                    .Where(mimeSpec => mimeSpec.type.IsRelevantFormatterFor(type))
                    .Select((x, index) => (x.type, x.mimeTypes, index))
                    .ToArray();

            if (candidates.Length > 0)
            {
                Array.Sort(candidates, new SortByRelevanceAndOrder<(Type type, HashSet<string> mimeTypes, int index)>(tup => tup.type, tup => tup.index));
                return candidates[0].mimeTypes;
            }
            else
            {
                return null;
            }
        }

        private static HashSet<string> InferMimeTypes(Type type) =>
            TryInferMimeTypes(type, _preferredMimeTypes)  ?? 
            new HashSet<string>{ TryInferMimeType(type, _defaultPreferredMimeTypes) ?? 
            _defaultMimeType};

        public static string ToDisplayString(
            this object obj,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (mimeType is null)
            {
                throw new ArgumentNullException(nameof(mimeType));
            }

            using var writer = CreateWriter();
            using (var context = new FormatContext(writer))
            {
                FormatTo(obj, context, mimeType);
            }

            return writer.ToString();
        }

        public static string ToDisplayString(
            this object obj,
            ITypeFormatter formatter)
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            using var writer = CreateWriter();
            using (var context = new FormatContext(writer))
            {
                formatter.Format(obj, context);
            }

            return writer.ToString();
        }

        public static void Format(this ITypeFormatter formatter, object instance, TextWriter writer)
        {
            using var context = new FormatContext(writer);
            formatter.Format(instance, context);
        }
        
        /// <summary>Invoke the formatter</summary>
        public static void FormatTo<T>(
            this T obj,
            FormatContext context,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (obj is not null)
            {
                var actualType = obj.GetType();

                if (typeof(T) != actualType)
                {
                    // in some cases the generic parameter is Object but the object is of a more specific type, in which case get or add a cached accessor to the more specific Formatter<T>.Format method
                    var genericFormatter = _genericFormattersTable.GetOrAdd(actualType, GetGenericFormatterMethod);
                    genericFormatter(context, obj, mimeType);
                    return;
                }
            }

            Formatter<T>.FormatTo(obj, context, mimeType);
        }

        internal static Action<FormatContext, object, string> GetGenericFormatterMethod(this Type type)
        {
            var methodInfo = typeof(Formatter<>)
                             .MakeGenericType(type)
                             .GetMethod(nameof(Formatter<object>.FormatTo), new[]
                             {
                                 type,
                                 typeof(FormatContext),
                                 typeof(string)
                             });

            var targetParam = Expression.Parameter(typeof(object), "target");
            var contextParam = Expression.Parameter(typeof(FormatContext), "context");
            var mimeTypeParam = Expression.Parameter(typeof(string), "mimeType");

            var methodCallExpr = Expression.Call(null,
                                                 methodInfo,
                                                 Expression.Convert(targetParam, type),
                                                 contextParam,
                                                 mimeTypeParam);

            return Expression.Lambda<Action<FormatContext, object, string>>(
                methodCallExpr,
                contextParam,
                targetParam,
                mimeTypeParam).Compile();
        }

        internal static void Join(
            IEnumerable list,
            TextWriter writer,
            FormatContext context,
            int? listExpansionLimit = null) =>
            JoinGeneric(list.Cast<object>(), writer, context, listExpansionLimit);

        internal static void JoinGeneric<T>(
            IEnumerable<T> seq,
            TextWriter writer,
            FormatContext context,
            int? listExpansionLimit = null)
        {
            if (seq is null)
            {
                writer.Write(NullString);
                return;
            }

            SingleLinePlainTextFormatter.WriteStartSequence(writer);

            listExpansionLimit ??= Formatter<T>.ListExpansionLimit;

            var (itemsToWrite, remainingCount) = seq.TakeAndCountRemaining(listExpansionLimit.Value);

            for (var i = 0; i < itemsToWrite.Count; i++)
            {
                var item = itemsToWrite[i];
                if (i < listExpansionLimit)
                {
                    // write out another item in the list
                    if (i > 0)
                    {
                        SingleLinePlainTextFormatter.WriteSequenceDelimiter(writer);
                    }

                    SingleLinePlainTextFormatter.WriteStartSequenceItem(writer);

                    item.FormatTo(context);
                }
            }

            if (remainingCount != 0)
            {
                writer.Write(" ... (");

                if (remainingCount is { })
                {
                    writer.Write($"{remainingCount} ");
                }

                writer.Write("more)");
            }

            SingleLinePlainTextFormatter.WriteEndSequence(writer);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting.
        /// </summary>
        public static void Register(ITypeFormatter formatter)
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            ClearComputedState();

            _userTypeFormatters.Push(formatter);
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        /// <param name="mimeType">The MimeType for this formatter. If it is not specified it defaults to <see cref="PlainTextFormatter.MimeType"/></param>
        public static void Register<T>(
            FormatDelegate<T> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Register(new AnonymousTypeFormatter<T>(formatter, mimeType));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <paramref name="type" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        /// <param name="type">The type the formatter is registered for.</param>
        /// <param name="mimeType">The MimeType for this formatter. If it is not specified it defaults to <see cref="PlainTextFormatter.MimeType"/></param>
        public static void Register(
            Type type,
            FormatDelegate<object> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Register(new AnonymousTypeFormatter<object>(formatter, mimeType, type));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <paramref name="type" />.
        /// </summary>
        /// <param name="formatter">The formatting action.</param>
        /// <param name="type">The type the formatter is registered for.</param>
        /// <param name="mimeType">The MimeType for this formatter. If it is not specified it defaults to <see cref="PlainTextFormatter.MimeType"/></param>
        public static void Register(
            Type type,
            Action<object, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Register(new AnonymousTypeFormatter<object>((value, context) =>
            {
                formatter(value, context.Writer); 
                return true;
            }, mimeType, type));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatting action.</param>
        /// <param name="mimeType">The MimeType for this formatter. If it is not specified it defaults to <see cref="PlainTextFormatter.MimeType"/></param>
        public static void Register<T>(
            Action<T, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Register(new AnonymousTypeFormatter<object>((value, context) =>
            {
                formatter((T)value, context.Writer); 
                return true;
            }, mimeType, typeof(T)));
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="formatter">The formatter.</param>
        /// <param name="mimeType">The MimeType for this formatter. If it is not specified it defaults to <see cref="PlainTextFormatter.MimeType"/></param>
        public static void Register<T>(
            Func<T, string> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            Register(new AnonymousTypeFormatter<T>((value, context) =>
            {
                context.Writer.Write(formatter(value)); 
                return true;
            }, mimeType));
        }

        public static IEnumerable<ITypeFormatter> RegisteredFormatters(bool includeDefaults = true)
        {
            foreach (var formatter in _userTypeFormatters)
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

        public static ITypeFormatter GetPreferredFormatterFor(Type actualType, string mimeType) =>
            _typeFormattersTable
                .GetOrAdd(
                    (actualType, mimeType),
                    tuple => InferPreferredFormatter(actualType, mimeType));

        internal static ITypeFormatter InferPreferredFormatter(Type actualType, string mimeType)
        {
            // Try to find a user-specified type formatter, use the most specific type with a matching mime type
            if (TryInferPreferredFormatter(actualType, mimeType, _userTypeFormatters) is { } userFormatter)
            {
                return userFormatter;
            }

            if (!_typesThatHaveBeenCheckedForFormatters.ContainsKey(actualType))
            {
                var foundFormatter = false;
                var customAttributes = actualType.GetCustomAttributes<TypeFormatterSourceAttribute>().ToArray();

                if (customAttributes.Length > 0)
                {
                    foundFormatter = TryRegisterFromWellKnownFormatterSources(customAttributes);
                }
                else
                {
                    foundFormatter = TryRegisterFromConventionBasedFormatterSources();
                }

                _typesThatHaveBeenCheckedForFormatters.TryAdd(actualType, foundFormatter);
            }

            // Try to find a default built-in type formatter, use the most specific type with a matching mime type
            if (TryInferPreferredFormatter(actualType, mimeType, _defaultTypeFormatters) is { } defaultFormatter)
            {
                return defaultFormatter;
            }

            // Last resort use preferred mimeType
            var preferredMimeType = GetPreferredMimeTypesFor(actualType).FirstOrDefault(m => m == mimeType);

            if (preferredMimeType != mimeType)
            {
                throw new ArgumentException($"No formatter is registered for MIME type {mimeType}.");
            }

            return GetPreferredFormatterFor(actualType, preferredMimeType);

            bool TryRegisterFromWellKnownFormatterSources(TypeFormatterSourceAttribute[] customAttributes)
            {
                bool foundFormatter = false;

                foreach (var formatterSourceAttribute in customAttributes)
                {
                    if (Activator.CreateInstance(formatterSourceAttribute.FormatterSourceType) is not ITypeFormatterSource source)
                    {
                        throw new InvalidOperationException($"The formatter source specified on '{actualType}' does not implement {nameof(ITypeFormatterSource)}");
                    }

                    var formatters = source.CreateTypeFormatters();

                    foreach (var formatter in formatters)
                    {
                        _defaultTypeFormatters.Push(formatter);
                        foundFormatter = true;
                    }
                }

                return foundFormatter;
            }

            bool TryRegisterFromConventionBasedFormatterSources()
            {
                bool foundFormatter = false;
                var attributesByConvention = actualType.GetCustomAttributes(true).Where(a => a.GetType().Name == nameof(TypeFormatterSourceAttribute));

                foreach (var attr in attributesByConvention)
                {
                    if (TryGetPropertyValue<object>(attr, nameof(TypeFormatterSourceAttribute.FormatterSourceType), out var prop) &&
                        prop is Type formatterSourceType)
                    {
                        var formatterSource = Activator.CreateInstance(formatterSourceType);

                        if (formatterSource.GetType()
                                           .GetMethod(nameof(ITypeFormatterSource.CreateTypeFormatters))
                                           .Invoke(formatterSource, null) is IEnumerable formatters)
                        {
                            foreach (var formatterByConvention in formatters)
                            {
                                if (TryGetPropertyValue<string>(formatterByConvention, nameof(ITypeFormatter.MimeType), out var mimeTyp))
                                {
                                    MethodInfo formatMethod = formatterByConvention.GetType().GetMethod(nameof(ITypeFormatter.Format));

                                    var formatterExpr = Expression.Constant(formatterByConvention);

                                    var valueToFormatExpr = Expression.Parameter(typeof(object));

                                    var writerExpr = Expression.Parameter(typeof(TextWriter));

                                    var methodCallExpression = Expression.Call(
                                        formatterExpr,
                                        formatMethod,
                                        new[]
                                        {
                                            valueToFormatExpr,
                                            writerExpr
                                        });

                                    var formatExpr = Expression.Lambda<Func<object, TextWriter, bool>>(
                                                                   methodCallExpression,
                                                                   valueToFormatExpr,
                                                                   writerExpr)
                                                               .Compile();

                                    if (!TryGetPropertyValue(formatterByConvention, nameof(ITypeFormatter.Type), out Type formattedType))
                                    {
                                        formattedType = actualType;
                                    }

                                    var formatter = new AnonymousTypeFormatter<object>(
                                        (value, context) => formatExpr(value, context.Writer), 
                                        mimeTyp, 
                                        formattedType);

                                    _defaultTypeFormatters.Push(formatter);

                                    foundFormatter = true;
                                }
                            }
                        }
                    }
                }

                return foundFormatter;
            }
        }

        internal static ITypeFormatter TryInferPreferredFormatter(Type actualType, string mimeType, IEnumerable<ITypeFormatter> formatters)
        {
            // Find the most specific type that specifies a mimeType
            var candidates =
                formatters
                    .Where(formatter => formatter.MimeType == mimeType && formatter.Type.IsRelevantFormatterFor(actualType))
                    .Select((x, i) => (formatter: x, index: i))
                    .ToArray();

            switch (candidates.Length)
            {
                case 1:
                    return candidates[0].formatter;

                case > 0:
                    Array.Sort(candidates, new SortByRelevanceAndOrder<(ITypeFormatter formatter, int index)>(tup => tup.formatter.Type, tup => tup.index));

                    // Compose the possible formatters into one formatter, trying each in turn
                    return new AnonymousTypeFormatter<object>((value, context) =>
                    {
                        for (var i = 0; i < candidates.Length; i++)
                        {
                            var formatter = candidates[i];
                            if (formatter.formatter.Format(value, context))
                            {
                                return true;
                            }
                        }

                        return false;
                    }, mimeType, candidates[0].formatter.Type);

                default:
                    return null;
            }
        }

        private static IReadOnlyCollection<T> ReadOnlyMemoryToArray<T>(ReadOnlyMemory<T> mem) => mem.Span.ToArray();

        internal static readonly MethodInfo FormatReadOnlyMemoryMethod =
            typeof(Formatter)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m => m.Name == nameof(ReadOnlyMemoryToArray));

        private static bool TryGetPropertyValue<T>(object fromObject, string propertyName, out T value)
        {
            if (fromObject.GetType().GetProperty(propertyName) is { } propInfo &&
                propInfo.GetGetMethod() is { } getMethod)
            {
                object valueObj = getMethod.Invoke(fromObject, null);

                if (valueObj is T valueT)
                {
                    value = valueT;

                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}