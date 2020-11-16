namespace Microsoft.DotNet.Interactive.Commands
{
    /// <summary>
    /// Applications can send and handle this command, using the payload to convey whatever they want.
    /// </summary>
    public class ApplicationCommand : KernelCommand
    {
        public ApplicationCommand(string label, object content)
        {
            Label = label;
            Content = content;
        }

        public string Label { get; }
        public object Content { get; }
    }
}
