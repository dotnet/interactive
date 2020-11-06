namespace Microsoft.DotNet.Interactive.Commands
{
    // TODO: is "Send" the right verb here? Should it be Handle? Process?
    public class SendMessage : KernelCommand
    {
        public SendMessage(string label, object content)
        {
            Label = label;
            Content = content;
        }

        public string Label { get; }
        public object Content { get; }
    }
}
