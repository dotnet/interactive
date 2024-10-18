using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using System.Threading.Tasks;
using System;

namespace WpfConnect;

public class DispatcherCommand : KernelCommand
{
    public DispatcherCommand(
        string enabled,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(enabled))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(enabled));
        }

        Enabled = enabled;
    }

    public string Enabled { get; }
}
