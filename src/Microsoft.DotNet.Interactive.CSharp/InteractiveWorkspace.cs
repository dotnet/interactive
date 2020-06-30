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
        private DocumentId _committedDocumentId;
        private DocumentId _workingDocumentId;

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

        public void AddSubmission(ScriptState scriptState)
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

            var currentSubmissionDocumentId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                debugName: debugName);

            // add the code submission to the current project
            var submissionSourceText = SourceText.From(scriptState.Script.Code);

            _solution = _solution.AddDocument(
                currentSubmissionDocumentId,
                debugName,
                submissionSourceText);

            if (_previousSubmissionProjectId != null)
            {
                _solution = _solution.AddProjectReference(
                    _currentSubmissionProjectId,
                    new ProjectReference(_previousSubmissionProjectId));
            }

            // remove rollup and working document from project

            if (_workingDocumentId != null)
            {
                _solution.Workspace.CloseDocument(_workingDocumentId);
                _solution = _solution.RemoveDocument(_workingDocumentId);
            }

            if (_committedDocumentId != null)
            {
               
                _solution = _solution.RemoveDocument(_committedDocumentId);
            }

            // create new ids and reuse buffers

            _committedTextContainer.AppendText(scriptState.Script.Code);
            _workingTextContainer.SetText(string.Empty);


            var workingProjectName = $"Rollup through #{_submissionCount - 1}";

            _committedDocumentId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                workingProjectName);

            _solution = _solution.AddDocument(
                _committedDocumentId,
                workingProjectName,
                TextLoader.From(_committedTextContainer, new VersionStamp()));

            var workingDocumentName = $"Fork from #{_submissionCount - 1}";
            
            _workingDocumentId = DocumentId.CreateNewId(
                _currentSubmissionProjectId,
                workingDocumentName);


            _solution = _solution.AddDocument(
                _workingDocumentId,
                workingDocumentName,
                TextLoader.From(_workingTextContainer, VersionStamp.Create())
                );

            _solution.Workspace.OpenDocument(_workingDocumentId,true);

            _languageServicesDocument = _solution.GetDocument(_workingDocumentId);

        }

        public Document ForkDocument(string code)
        {
            _workingTextContainer.SetText(code);
            return _languageServicesDocument;

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
            if (TextChanged is { } e)
            {
                e.Invoke(this, new TextChangeEventArgs(old, _currentText));
            }
        }

        public void AppendText(string text)
        {
            var old = _currentText;
            _currentText = _currentText.Replace(_currentText.Length,text.Length, text);
            if (TextChanged is {} e)
            {
                e.Invoke(this, new TextChangeEventArgs(old, _currentText));
            }
        }

        public override SourceText CurrentText =>  GetSourceText();

        private SourceText GetSourceText()
        {
            return _currentText;
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;
    }

}