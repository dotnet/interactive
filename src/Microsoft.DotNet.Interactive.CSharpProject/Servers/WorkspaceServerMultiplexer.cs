using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers
{
    public class WorkspaceServerMultiplexer : IWorkspaceServer
    {
        private IPackageFinder _packageFinder;
        private readonly IWorkspaceServer _roslynWorkspaceServer;

        public WorkspaceServerMultiplexer(IPackageFinder packageFinder)
        {
            _packageFinder = packageFinder;
            _roslynWorkspaceServer = new RoslynWorkspaceServer(packageFinder);
        }

        public Task<CompileResult> Compile(WorkspaceRequest request, Budget budget = null)
        {
            return _roslynWorkspaceServer.Compile(request, budget);
        }

        public Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null)
        {
            return _roslynWorkspaceServer.GetCompletionList(request, budget);
        }

        public Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null)
        {
            return _roslynWorkspaceServer.GetDiagnostics(request, budget);
        }

        public Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null)
        {
            return _roslynWorkspaceServer.GetSignatureHelp(request, budget);
        }

        public Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            return _roslynWorkspaceServer.Run(request, budget);
        }
    }
}
