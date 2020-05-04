// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Recipes;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public class SignatureValidator
    {
        private readonly HMAC _signatureGenerator;
        private readonly Encoding _encoder;

        public SignatureValidator(string key, string algorithm)
        {
            _encoder = new UTF8Encoding();
            _signatureGenerator = HMAC.Create(algorithm);
            _signatureGenerator.Key = _encoder.GetBytes(key);
        }

        public string CreateSignature(Message message)
        {
            var messages = GetMessagesToAddForDigest(message);

            // For all items update the signature
            foreach (var item in messages)
            {
                var sourceBytes = _encoder.GetBytes(item);
                _signatureGenerator.TransformBlock(sourceBytes, 0, sourceBytes.Length, null, 0);
            }

            _signatureGenerator.TransformFinalBlock(new byte[0], 0, 0);

            // Calculate the digest and remove -
            return BitConverter.ToString(_signatureGenerator.Hash).Replace("-", "").ToLower();
        }

        private static IEnumerable<string> GetMessagesToAddForDigest(Message message)
        {
            yield return message.Header.ToJson();
            yield return message.ParentHeader.ToJson();
            yield return message.MetaData.ToJson();
            yield return message.Content.ToJson();
        }
    }
}