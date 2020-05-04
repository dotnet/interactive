// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    ///   A dynamic object representing HTML attributes.
    /// </summary>
    public class HtmlAttributes : DynamicObject, IDictionary<string, object>, IHtmlContent
    {
        private readonly SortedDictionary<string, object> _attributes = new SortedDictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        ///   Initializes a new instance of the <see cref = "HtmlAttributes" /> class.
        /// </summary>
        public HtmlAttributes()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "HtmlAttributes" /> class.
        /// </summary>
        /// <param name = "attributes">Key/value pairs to be added to the HtmlAttributes instance.</param>
        public HtmlAttributes(IEnumerable<KeyValuePair<string, object>> attributes)
        {
            InitializeFrom(attributes);
        }

        private void InitializeFrom(IEnumerable<KeyValuePair<string, object>> attributes)
        {
            foreach (var item in attributes)
            {
                _attributes.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///   A <see cref = "T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _attributes.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///   An <see cref = "T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Adds an item to the <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name = "item">The object to add to the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        ///   Adds CSS classes to the attributes.
        /// </summary>
        /// <param name = "value">A space-delimited list of CSS classes to be added.</param>
        public void AddCssClass(string value)
        {
            if (TryGetValue("class", out var values))
            {
                _attributes["class"] = value + " " + values;
            }
            else
            {
                _attributes["class"] = value;
            }
        }

        /// <summary>
        ///   Removes classes from the attributes.
        /// </summary>
        /// <param name = "value">A space-delimited list of CSS classes to be removed.</param>
        public void RemoveCssClass(string value)
        {
            if (TryGetValue("class", out var currentObj))
            {
                var currentClasses = currentObj.ToString();
                _attributes["class"] = string.Join(" ", currentClasses.Split(' ').Except(value.Split(' ')));
            }
        }

        /// <summary>
        ///   Removes all items from the <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public void Clear()
        {
            _attributes.Clear();
        }

        /// <summary>
        ///   Determines whether the <see cref = "T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name = "item">The object to locate in the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///   true if <paramref name = "item" /> is found in the <see cref = "T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        /// </returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
            return _attributes.Contains(item);
        }

        /// <summary>
        ///   Copies the elements of the <see cref = "T:System.Collections.Generic.ICollection`1" /> to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name = "array">The one-dimensional System.Array that is the destination of the elements copied from <see
        ///    cref = "T:System.Collections.Generic.ICollection`1" />. The System.Array must have zero-based indexing.</param>
        /// <param name = "arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>) _attributes).CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///   Removes the first occurrence of a specific object from the <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name = "item">The object to remove from the <see cref = "T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        ///   true if <paramref name = "item" /> was successfully removed from the <see
        ///    cref = "T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref
        ///    name = "item" /> is not found in the original <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </exception>
        public bool Remove(KeyValuePair<string, object> item)
        {
            return _attributes.Remove(item.Key);
        }

        /// <summary>
        ///   Gets the number of elements contained in the <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <returns>
        ///   The number of elements contained in the <see cref = "T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        public int Count => _attributes.Count;

        /// <summary>
        ///   Gets a value indicating whether the <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref = "T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly => false;

        /// <summary>
        ///   Determines whether the <see cref = "T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
        /// </summary>
        /// <param name = "key">The key to locate in the <see cref = "T:System.Collections.Generic.IDictionary`2" />.</param>
        /// <returns>
        ///   true if the <see cref = "T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "key" /> is null.
        /// </exception>
        public bool ContainsKey(string key) => _attributes.ContainsKey(key);

        /// <summary>
        ///   Adds an element with the provided key and value to the <see cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name = "key">The object to use as the key of the element to add.</param>
        /// <param name = "value">The object to use as the value of the element to add.</param>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "key" /> is null.
        /// </exception>
        /// <exception cref = "T:System.ArgumentException">
        ///   An element with the same key already exists in the <see cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The <see cref = "T:System.Collections.Generic.IDictionary`2" /> is read-only.
        /// </exception>
        public void Add(string key, object value) => _attributes.Add(key, value);

        /// <summary>
        ///   Removes the element with the specified key from the <see cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name = "key">The key of the element to remove.</param>
        /// <returns>
        ///   true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref
        ///    name = "key" /> was not found in the original <see cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "key" /> is null.
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The <see cref = "T:System.Collections.Generic.IDictionary`2" /> is read-only.
        /// </exception>
        public bool Remove(string key) => _attributes.Remove(key);

        /// <summary>
        ///   Gets the value associated with the specified key.
        /// </summary>
        /// <param name = "key">The key whose value to get.</param>
        /// <param name = "value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref
        ///    name = "value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        ///   true if the object that implements <see cref = "T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "key" /> is null.
        /// </exception>
        public bool TryGetValue(string key, out object value) => 
            _attributes.TryGetValue(key, out value);

        /// <summary>
        ///   Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        ///   The element with the specified key.
        /// </returns>
        /// <exception cref = "T:System.ArgumentNullException"><paramref name = "key" /> is null.
        /// </exception>
        /// <exception cref = "T:System.Collections.Generic.KeyNotFoundException">
        ///   The property is retrieved and <paramref name = "key" /> is not found.
        /// </exception>
        /// <exception cref = "T:System.NotSupportedException">
        ///   The property is set and the <see cref = "T:System.Collections.Generic.IDictionary`2" /> is read-only.
        /// </exception>
        public object this[string key]
        {
            get => _attributes[key];
            set => _attributes[key] = value;
        }

        /// <summary>
        ///   Gets an <see cref = "T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see
        ///    cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>
        ///   An <see cref = "T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see
        ///    cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </returns>
        public ICollection<string> Keys => _attributes.Keys;

        /// <summary>
        ///   Gets an <see cref = "T:System.Collections.Generic.ICollection`1" /> containing the values in the <see
        ///    cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>
        ///   An <see cref = "T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see
        ///    cref = "T:System.Collections.Generic.IDictionary`2" />.
        /// </returns>
        public ICollection<object> Values => _attributes.Values;

        /// <summary>
        ///   Provides the implementation for operations that get member values. Classes derived from the <see
        ///    cref = "T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name = "binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see
        ///    cref = "T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name = "result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref
        ///    name = "result" />.</param>
        /// <returns>
        ///   true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) => _attributes.TryGetValue(binder.Name, out result);

        /// <summary>
        ///   Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>
        ///   A sequence that contains dynamic member names.
        /// </returns>
        public override IEnumerable<string> GetDynamicMemberNames() => _attributes.Keys;

        /// <summary>
        ///   Provides the implementation for operations that set member values. Classes derived from the <see
        ///    cref = "T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name = "binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see
        ///    cref = "T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name = "value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see
        ///    cref = "T:System.Dynamic.DynamicObject" /> class, the <paramref name = "value" /> is "Test".</param>
        /// <returns>
        ///   true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _attributes[binder.Name] = value;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            switch (binder.Name)
            {
                case nameof(MergeWith):
                    var dictionary =
                        binder
                            .CallInfo
                            .ArgumentNames
                            .Select(
                                (name, index) => new KeyValuePair<string, object>(name, args[index]))
                            .ToDictionary(kvp => kvp.Key,
                                          kvp => kvp.Value);
                    MergeWith(dictionary);
                    result = this;
                    return true;
                default:
                    result = false;
                    return false;
            }
        }

        /// <summary>
        ///   Merges specified attributes the with the current instance.
        /// </summary>
        /// <param name = "htmlAttributes">The attributes to be merged.</param>
        /// <param name = "replace">if set to <c>true</c> [replace].</param>
        public void MergeWith(IDictionary<string, object> htmlAttributes, bool replace = false) => 
            _attributes.MergeWith(htmlAttributes, replace);
    
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var pair in _attributes)
            {
                // don't write out empty id attributes
                if (string.Equals(pair.Key, "id", StringComparison.Ordinal) &&
                    (pair.Value == null || pair.Value.ToString() == string.Empty))
                {
                    continue;
                }

                if (!first)
                {
                    // spaces between attributes
                    sb.Append(" ");
                }
                else
                {
                    first = false;
                }

                if (pair.Value is JsonString)
                {
                    // don't re-encode, use ' as delimiter
                    sb.Append(pair.Key)
                      .Append("='")
                      .Append(pair.Value)
                      .Append("'");
                }
                else
                {
                    sb.Append(pair.Key)
                      .Append("=\"")
                      .Append(pair.Value.EnsureHtmlAttributeEncoded())
                      .Append("\"");
                }
            }

            return sb.ToString();
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (Count > 0)
            {
                writer.Write(" ");
                writer.Write(ToString());
            }
        }
    }
}