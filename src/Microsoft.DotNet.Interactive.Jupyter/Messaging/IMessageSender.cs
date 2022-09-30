using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    public interface IMessageSender
    {
        Task SendAsync(Message message);
    }
}
