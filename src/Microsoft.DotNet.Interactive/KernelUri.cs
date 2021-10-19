// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class KernelUri
    {
        protected bool Equals(KernelUri other)
        {
            return StringComparer.Ordinal.Equals( _stringValue, other._stringValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((KernelUri) obj);
        }

        public override int GetHashCode()
        {
            return (_stringValue is not null ? _stringValue.GetHashCode() : 0);
        }

        private readonly string _stringValue;

        private KernelUri(string kernelUri)
        {
            if (string.IsNullOrWhiteSpace(kernelUri))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(kernelUri));
            }

            Parts = kernelUri.Split('/');
            _stringValue = kernelUri;
        }

        public override string ToString()
        {
            return _stringValue;
        }

        public static KernelUri Parse(string kernelUri)
        {
            return new(kernelUri);

        }

        public string[] Parts { get; }

        public KernelUri Append(string part)
        {
            return new($"{_stringValue}/{part}");
        }


        public bool Contains(KernelUri uri)
        {
            if (uri.Parts.Length > Parts.Length){
                return false;
            }

            for (var i = 0; i < uri.Parts.Length; i++)
            {
                if(!StringComparer.Ordinal.Equals(Parts[i],uri.Parts[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}