namespace Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms
{
    internal interface ICommTarget
    {
        public string Name { get; }

        public void OnCommOpen(CommAgent commAgent, object data);
    }
}
