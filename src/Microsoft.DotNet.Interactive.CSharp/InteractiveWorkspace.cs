﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
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
        private Document _languageServicesDocument;
        private TextContainer _workingTextContainer;
        private TextContainer _committedTextContainer;

        public InteractiveWorkspace()
        {
            var workspace = new AdhocWorkspace(MefHostServices.DefaultHost, WorkspaceKind.Interactive);
            
            _workingTextContainer = new TextContainer();
            _committedTextContainer = new TextContainer();

            _solution = workspace.CurrentSolution;

            _parseOptions = new CSharpParseOptions(
                LanguageVersion.Latest,
                DocumentationMode.None,
                SourceCodeKind.Script);

            _disposables.Add(workspace);

            _disposables.Add(Disposable.Create(() =>
            {
                _committedTextContainer = null;
                _workingTextContainer = null;
                _currentCompilation = null;
                _solution = null;
            }));
        }

        public async Task AddSubmissionAsync(ScriptState scriptState)
        {
            _currentCompilation = scriptState.Script.GetCompilation();

            _previousSubmissionProjectId = _currentSubmissionProjectId;

            _committedTextContainer.AppendText(scriptState.Script.Code);
            _workingTextContainer.SetText(string.Empty);

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

            var submissionSourceText = SourceText.From(scriptState.Script.Code);

            _solution = _solution.AddDocument(
                documentId,
                debugName,
                submissionSourceText);

            if (_previousSubmissionProjectId != null)
            {
                _solution = _solution.AddProjectReference(
                    _currentSubmissionProjectId,
                    new ProjectReference(_previousSubmissionProjectId));
            }
           
            var rollupName = $"Rollup through #{_submissionCount - 1}";

            var rollupDocId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                rollupName);

            _uncommittedSolution = _solution.AddDocument(
                rollupDocId,
                rollupName,
                await CollateSubmissionsAsync());

            var completionDocName = $"Fork from #{_submissionCount - 1}";
            
            var completionDocId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                completionDocName);
            
            
            _uncommittedSolution = _uncommittedSolution.AddDocument(
                completionDocId,
                completionDocName,
                TextLoader.From(_workingTextContainer, VersionStamp.Create())
                );
            
            _uncommittedSolution.Workspace.OpenDocument(completionDocId,true);

            _languageServicesDocument = _uncommittedSolution.GetDocument(completionDocId);


        }

        public Document ForkDocument(string code)
        {
            _workingTextContainer.SetText(code);
            return _languageServicesDocument;

        }

        private async Task<string> CollateSubmissionsAsync()
        {
            var sb = new StringBuilder();

            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var text = await document.GetTextAsync();

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

    internal class TextContainer : SourceTextContainer
    {
        private SourceText _currentText;

        public TextContainer()
        {
            _currentText = SourceText.From(string.Empty);
        }

        public void SetText(string text)
        {
            var old = _currentText;
            _currentText = SourceText.From(text);
            TextChanged?.Invoke(this, new TextChangeEventArgs(old,_currentText));
        }

        public void AppendText(string text)
        {
            var old = _currentText;
            _currentText = _currentText.Replace(_currentText.Length,text.Length, text);
            TextChanged?.Invoke(this, new TextChangeEventArgs(old, _currentText));
        }

        public override SourceText CurrentText => _currentText;
        public override event EventHandler<TextChangeEventArgs> TextChanged;
    }

}