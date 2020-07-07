// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reactive.Disposables;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal class InteractiveWorkspace : Workspace
    {
        private ProjectId _previousSubmissionProjectId;
        private ProjectId _workingProjectId;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private int _submissionCount;
        private readonly CSharpParseOptions _parseOptions;
        private Compilation _currentCompilation;
        private DocumentId _workingDocumentId;

        public InteractiveWorkspace() : base(MefHostServices.DefaultHost, WorkspaceKind.Interactive)
        {
            _parseOptions = new CSharpParseOptions(
                LanguageVersion.Latest,
                DocumentationMode.None,
                SourceCodeKind.Script);

            _disposables.Add(Disposable.Create(() =>
            {
                _currentCompilation = null;
            }));
        }

        public void UpdateWorkspace(ScriptState scriptState)
        {
            _currentCompilation = scriptState.Script.GetCompilation();

            var solution = CurrentSolution;
            solution = RemoveWorkingProjectFromSolution(solution);

            SetCurrentSolution(solution);

            _previousSubmissionProjectId = AddProjectWithPreviousSubmissionToSolution(_currentCompilation, scriptState.Script.Code, _workingProjectId, _previousSubmissionProjectId);

            (_workingProjectId, _workingDocumentId) = AddNewWorkingProjectToSolution(_currentCompilation, _previousSubmissionProjectId);
        }

        private Solution RemoveWorkingProjectFromSolution(Solution solution)
        {
            if (_workingProjectId != null)
            {
                solution = solution.RemoveProject(_workingProjectId);
            }

            return solution;
        }

        private (ProjectId projectId, DocumentId workingDocumentId) AddNewWorkingProjectToSolution(Compilation previousCompilation, ProjectId projectReferenceProjectId)
        {
            var solution = CurrentSolution;
            var assemblyName = $"Submission#{_submissionCount++}";
            var compilationOptions = previousCompilation.Options.WithScriptClassName(assemblyName);

            var projectId = ProjectId.CreateNewId(debugName: $"workingProject{assemblyName}");

            solution = CreateProjectAndAddToSolution(
                projectId, 
                assemblyName, 
                compilationOptions, 
                solution,
                projectReferenceProjectId);

            var workingDocumentId = DocumentId.CreateNewId(
                projectId);

            solution = solution.AddDocument(
                workingDocumentId,
                "active",
                string.Empty);

            SetCurrentSolution(solution);
            return (projectId, workingDocumentId);
        }

        private Solution CreateProjectAndAddToSolution(
            ProjectId projectId, 
            string assemblyName, 
            CompilationOptions compilationOptions, 
            Solution solution, 
            ProjectId projectReferenceProjectId, 
            IEnumerable<MetadataReference> metadataReferences = null)
        {
            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: assemblyName,
                assemblyName: assemblyName,
                language: LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: compilationOptions,
                metadataReferences: metadataReferences,
                isSubmission: true);

            solution = solution.AddProject(projectInfo);

            if (projectReferenceProjectId != null)
            {
                solution = solution.AddProjectReference(
                    projectId,
                    new ProjectReference(projectReferenceProjectId)
                );
            }

            return solution;
        }

        private ProjectId AddProjectWithPreviousSubmissionToSolution(Compilation compilation, string code, ProjectId projectId, ProjectId projectReferenceProjectId)
        {
            var solution = CurrentSolution;

            var compilationOptions = compilation.Options;
            var assemblyName = compilationOptions.ScriptClassName;

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = $"Submission#{_submissionCount}";
                compilationOptions = compilationOptions.WithScriptClassName(assemblyName);
            }

            var debugName = assemblyName;
#if DEBUG
            debugName += $": {code}";
#endif
            if (projectId == null)
            {
                projectId = ProjectId.CreateNewId(debugName: debugName);
            }

            solution = CreateProjectAndAddToSolution(
                projectId, 
                assemblyName, 
                compilationOptions, 
                solution,
                projectReferenceProjectId, 
                compilation.References);

            var currentSubmissionDocumentId = DocumentId.CreateNewId(
                projectId,
                debugName: assemblyName);

            // add the code submission to the current project
            var submissionSourceText = SourceText.From(code);

            solution = solution.AddDocument(
                currentSubmissionDocumentId,
                debugName,
                submissionSourceText);

            SetCurrentSolution(solution);
            return projectId;
        }


        public Document UpdateWorkingDocument(string code)
        {
            var solution = CurrentSolution;
            solution = solution.RemoveDocument(_workingDocumentId);

            var documentDebugName = $"Working from #{_submissionCount}";

            _workingDocumentId = DocumentId.CreateNewId(
                _workingProjectId,
                documentDebugName);

            solution = solution.AddDocument(
                _workingDocumentId,
                documentDebugName,
                SourceText.From(code)
            );

            var workingDocument = solution.GetDocument(_workingDocumentId);
            SetCurrentSolution(solution);
            return workingDocument;

        }

        protected override void Dispose(bool finalize)
        {
            if (!finalize)
            {
                _disposables.Dispose();
            }
            base.Dispose(finalize);
        }
    }
}