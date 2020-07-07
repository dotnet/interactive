// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private ProjectId _currentSubmissionProjectId;
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

        public void AddSubmission(ScriptState scriptState)
        {
            _currentCompilation = scriptState.Script.GetCompilation();

            var solution = CurrentSolution;
            if (_currentSubmissionProjectId != null)
            {
                solution = solution.RemoveProject(_currentSubmissionProjectId);
            }

            SetCurrentSolution(solution);

            _previousSubmissionProjectId = CreateProjectForPreviousSubmission(_currentCompilation, scriptState.Script.Code, _currentSubmissionProjectId, _previousSubmissionProjectId);

            (_currentSubmissionProjectId, _workingDocumentId) = CreateProjectForCurrentSubmission(_currentCompilation, _previousSubmissionProjectId);
        }

        private (ProjectId projectId, DocumentId workingDocumentId) CreateProjectForCurrentSubmission(Compilation previousCompilation, ProjectId projectReferenceProjectId)
        {
            var submission = _submissionCount++;
            var solution = CurrentSolution;
            var assemblyName = $"Submission#{submission}";
            var compilationOptions = previousCompilation.Options.WithScriptClassName(assemblyName);
            var debugName = assemblyName;
            var projectId = ProjectId.CreateNewId(debugName: debugName);

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: debugName,
                assemblyName: assemblyName,
                language: LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: compilationOptions,
                isSubmission: true);
            
            solution = solution.AddProject(projectInfo);

            if (projectReferenceProjectId != null)
            {
                solution = solution.AddProjectReference(
                    projectId,
                    new ProjectReference(projectReferenceProjectId)
                );
            }


            var workingDocumentId = DocumentId.CreateNewId(
                projectInfo.Id,
                debugName: $"working document for {submission}");

            solution = solution.AddDocument(
                workingDocumentId,
                $"working document for {submission}", 
                string.Empty);

            SetCurrentSolution(solution);
            return (projectId, workingDocumentId);
        }

        private ProjectId CreateProjectForPreviousSubmission(Compilation compilation, string code, ProjectId projectId, ProjectId projectReferenceProjectId)
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

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: debugName,
                assemblyName: assemblyName,
                language: LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: compilationOptions,
                metadataReferences: compilation.References,
                isSubmission:true);

            var currentSubmissionDocumentId = DocumentId.CreateNewId(
                projectInfo.Id,
                debugName: assemblyName);

            // add the code submission to the current project
            var submissionSourceText = SourceText.From(code);

            solution = solution.AddProject(projectInfo);
           
            if (projectReferenceProjectId != null)
            {
                solution = solution.AddProjectReference(
                    projectId,
                    new ProjectReference(projectReferenceProjectId)
                );
            }

            solution = solution.AddDocument(
                currentSubmissionDocumentId,
                debugName,
                submissionSourceText);

            SetCurrentSolution(solution);
            return projectId;
        }


        public Document ForkDocument(string code)
        {
            var solution = CurrentSolution;
            solution = solution.RemoveDocument(_workingDocumentId);

            var workingDocumentName = $"Fork from #{_submissionCount}";
            
            _workingDocumentId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                workingDocumentName);

            solution = solution.AddDocument(
                _workingDocumentId,
                workingDocumentName,
                SourceText.From(code)
            );

            var languageServicesDocument =
                solution.GetDocument(_workingDocumentId);
            SetCurrentSolution(solution);
            return languageServicesDocument;

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