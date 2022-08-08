using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal static class ValueAdapterMessageExtensions
    {
        public static ValueAdapterMessage FromDataDictionary(IReadOnlyDictionary<string, object> data)
        {
            if (data == null || !data.TryGetValue("type", out object messageType)) {
                return null;
            }

            if (messageType?.ToString() == ValueAdapterMessageType.Request || messageType?.ToString() == ValueAdapterMessageType.Response)
            {
                return ValueAdapterCommandMessage.FromDataDictionary(data);
            }

            if (messageType?.ToString() == ValueAdapterMessageType.Event)
            {
                return ValueAdapterEvent.FromDataDictionary(data);
            }

            return null;
        }

    }
}
