// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

public class SignatureValidator
{
    private readonly HMAC _signatureGenerator;
    private readonly object _gate = new();

    public Encoding Encoding { get; }

    public SignatureValidator(string key, string algorithm)
    {
        Encoding = new UTF8Encoding();
        _signatureGenerator = (HMAC)CryptoConfig.CreateFromName(algorithm);
        _signatureGenerator!.Key = Encoding.GetBytes(key);
    }

    public string CreateSignature(params byte[][] data)
    {
        lock (_gate)
        {
            // The signature generator is stateful, so we need to lock. Also need to
            // initialize in case an exception was thrown in a previous call.
            _signatureGenerator.Initialize();

            // For all items update the signature.
            foreach (var item in data)
                _signatureGenerator.TransformBlock(item, 0, item.Length, null, 0);

            _signatureGenerator.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            // Convert the hash, remove '-', and map to lower case.
            return BitConverter.ToString(_signatureGenerator!.Hash!).Replace("-", "").ToLowerInvariant();
        }
    }
}