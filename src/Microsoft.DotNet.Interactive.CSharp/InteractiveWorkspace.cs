// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal class InteractiveWorkspace : IDisposable
    {
        private ProjectId _previousSubmissionProjectId;
        private ProjectId _currentSubmissionProjectId;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private int _submissionCount;
        private readonly CSharpParseOptions _parseOptions;
        private Compilation _currentCompilation;
        private Solution _solution;

        public InteractiveWorkspace()
        {
            var workspace = new AdhocWorkspace(MefHostServices.DefaultHost, WorkspaceKind.Interactive);
            
            _solution = workspace.CurrentSolution;

            _parseOptions = new CSharpParseOptions(
                LanguageVersion.Latest,
                DocumentationMode.None,
                SourceCodeKind.Script);

            _disposables.Add(workspace);
        }

        public void AddSubmission(ScriptState scriptState)
        {
            _currentCompilation = scriptState.Script.GetCompilation();

            _previousSubmissionProjectId = _currentSubmissionProjectId;

            var assemblyName = $"Submission#{_submissionCount++}";
            var debugName = assemblyName;

#if DEBUG
            debugName += $": {scriptState.Script.Code}";
#endif

            _currentSubmissionProjectId = ProjectId.CreateNewId(debugName: debugName);

            var projectInfo = ProjectInfo.Create(
                _currentSubmissionProjectId,
                VersionStamp.Create(),
                name: debugName,
                assemblyName: assemblyName,
                language: LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: _currentCompilation.Options,
                metadataReferences: _currentCompilation.References);

            _solution = _solution.AddProject(projectInfo);

            var documentId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                debugName: debugName);

            _solution = _solution.AddDocument(
                documentId,
                debugName,
                SourceText.From(scriptState.Script.Code));

            if (_previousSubmissionProjectId != null)
            {
                _solution = _solution.AddProjectReference(
                    _currentSubmissionProjectId,
                    new ProjectReference(_previousSubmissionProjectId));
            }
        }

        public async Task<Document> ForkDocumentAsync(string code)
        {
            var completionDocName = $"Fork from #{_submissionCount - 1}: {code}";
            var completionDocId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                completionDocName);

            var rollupName = $"Rollup through #{_submissionCount - 1}";
            var rollupOfPreviousCode = await CollateTextAsync();

            var rollupDocId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                rollupName);

            var solution = _solution
                           .AddDocument(
                               completionDocId,
                               completionDocName,
                               SourceText.From(code))
                           .AddDocument(
                               rollupDocId,
                               rollupName,
                             SourceText.From(rollupOfPreviousCode));

            return solution.GetDocument(completionDocId);
        }

        private async Task<string> CollateTextAsync()
        {
            var sb = new StringBuilder();

            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var text =await document.GetTextAsync();

                    if (text != null)
                    {
                        sb.AppendLine(text.ToString());
                    }
                }
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}