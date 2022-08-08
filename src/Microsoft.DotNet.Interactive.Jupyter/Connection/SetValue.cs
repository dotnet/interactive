using Microsoft.DotNet.Interactive.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class SetValue : KernelCommand
    {
        public object Value { get; }

        public string Name { get; }
        public FormattedValue FormattedValue { get; }

        public SetValue(object value,
            string name,
            FormattedValue formattedValue)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Value = value;
            Name = name;
            FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));
        }
    }
}
