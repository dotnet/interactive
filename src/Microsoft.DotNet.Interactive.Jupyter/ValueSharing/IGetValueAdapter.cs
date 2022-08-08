using Microsoft.DotNet.Interactive.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal interface IGetValueAdapter
    {
        Task<IValueAdapter> GetValueAdapter(KernelInfo kernelInfo);
    }
}
