using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    [ValueAdapterEvent(ValueAdapterEventTypes.Initialized)]
    public class InitializedEvent : ValueAdapterEvent
    {
        public InitializedEvent() : base()
        {
        }
    }
}
