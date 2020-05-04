// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelCommandExtensions
    {
        internal const string TokenKey = "token";

        public static void SetToken(
            this IKernelCommand command,
            string token)
        {
            if (!command.Properties.TryGetValue(TokenKey, out var existing))
            {
                command.Properties.Add(TokenKey, new TokenSequence(token));
            }
            else if (!(existing is TokenSequence sequence) || sequence.Current != token)
            {
                throw new InvalidOperationException("Command token cannot be changed.");
            }
        }

        public static string GetToken(this IKernelCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.Properties.TryGetValue(TokenKey, out var value) &&
                value is TokenSequence tokenSequence)
            {
                return tokenSequence.Current;
            }

            if (command is KernelCommandBase commandBase &&
                commandBase.Parent != null)
            {
                var token = commandBase.Parent.GetToken();
                command.SetToken(token);
                return token;
            }

            return command.GenerateToken();
        }

        private static string GetNextToken(this IKernelCommand command)
        {
            if (command.Properties.TryGetValue(TokenKey, out var value) &&
                value is TokenSequence tokenSequence)
            {
                return tokenSequence.GetNext();
            }

            return command.GenerateToken();
        }

        private static string GenerateToken(this IKernelCommand command)
        {
            var seed = command is KernelCommandBase b
                           ? b?.Parent?.GetNextToken()
                           : null;

            var sequence = new TokenSequence(seed);

            command.Properties.Add(TokenKey, sequence);

            return sequence.Current;
        }

        private class TokenSequence
        {
            private readonly object _lock = new object();

            public TokenSequence(string current = null)
            {
                if (current != null)
                {
                    Current = current;
                }
                else
                {
                    Current = Hash(Guid.NewGuid().ToString());
                }
            }

            internal string Current { get; private set; }

            public string GetNext()
            {
                string next;

                lock (_lock)
                {
                    next = Current = Hash(Current);
                }

                return next;
            }

            private static string Hash(string seed)
            {
                var inputBytes = Encoding.ASCII.GetBytes(seed);

                byte[] hash;
                using (var sha = SHA256.Create())
                {
                    hash = sha.ComputeHash(inputBytes);
                }

                return Convert.ToBase64String(hash);
            }
        }
    }
}