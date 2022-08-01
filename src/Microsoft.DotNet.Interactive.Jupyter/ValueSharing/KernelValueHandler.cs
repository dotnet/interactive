using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class KernelValueHandler
    {
        public IValueSupport GetValueSupport(string languageName, IMessageSender sender, IMessageReceiver receiver)
        {
            switch(languageName)
            {
                case (LanguageNameValues.Python):
                    return new PythonValueSupport(sender, receiver);
                case (LanguageNameValues.R):
                    return new RValueSupport(sender, receiver);
                default:
                    // default null. Kernel does not support value sharing
                    return null;
            }
            
        }
    }
}
