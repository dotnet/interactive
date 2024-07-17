// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

internal abstract class SyntaxSpecBase<T> : ISyntaxSpec
    where T : SyntaxNode
{
    private readonly Action<T>[] _assertions;

    protected SyntaxSpecBase(string text, params Action<T>[] assertions)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _assertions = assertions;
    }

    protected SyntaxSpecBase(params Action<T>[] assertions)
    {
        _assertions = assertions;
    }

    public Random Randomizer { get; set; }

    public string Text { get; }

    public virtual void Validate(T syntaxNode)
    {
        if (Text is not null)
        {
            syntaxNode.Text.Should().Be(Text, because: $"{syntaxNode.GetType()}.Text");
        }

        if (_assertions is not null)
        {
            foreach (var assertion in _assertions)
            {
                assertion.Invoke(syntaxNode);
            }
        }
    }

    public void Validate(object syntaxNode)
    {
        syntaxNode.Should().BeOfType<T>();
        Validate((T)syntaxNode);
    }

    protected string MaybeWhitespace() =>
        Randomizer?.NextDouble() switch
        {
            < .2  => " ",
            > .2 and < .4 => "  ",
            > .4 and < .6 => "\t",
            > .6 and < .8 => "\t ",
            _ => ""
        };

    protected string MaybeNewLines() =>
        Randomizer?.NextDouble() switch
        {
            < .2 => "\n",
            > .2 and < .4 => "\r\n",
            _ => ""
        };

    public override string ToString() => Text;
}