// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public class AnnotatedCodeBlock : FencedCodeBlock
    {
        private string _sourceCode;
        private bool _initialized;

        public AnnotatedCodeBlock(
            BlockParser parser = null,
            int order = 0)
            : base(
                parser ?? new AnnotatedCodeBlockParser(new CodeFenceAnnotationsParser()))
        {
            Order = order;
        }

        public CodeFenceAnnotations Annotations { get; internal set; }

        public List<string> Diagnostics { get; } = new();

        public int Order { get; }

        public virtual async Task InitializeAsync()
        {
            if (Annotations == null && Diagnostics.Count == 0)
            {
                throw new InvalidOperationException("Attempted to initialize block before parsing code fence annotations");
            }

            if (_initialized)
            {
                return;
            }

            _initialized = true;

            switch (Annotations)
            {
                case CodeBlockAnnotations codeBlockAnnotations:
                    var result = await codeBlockAnnotations.TryGetExternalContent();

                    switch (result)
                    {
                        case SuccessfulCodeBlockContentFetchResult success:
                            SourceCode = success.Content;
                            await AddAttributes(codeBlockAnnotations);
                            break;

                        case ExternalContentNotEnabledResult _:
                            SourceCode = Lines.ToString();
                            await AddAttributes(codeBlockAnnotations);
                            break;

                        case FailedCodeBlockContentFetchResult failed:
                            Diagnostics.AddRange(failed.ErrorMessages);
                            break;
                    }

                    break;
            }
        }

        private void AddAttributeIfNotNull(string name, object value)
        {
            if (value != null)
            {
                AddAttribute(name, value.ToString());
            }
        }

        private protected virtual async Task AddAttributes(CodeBlockAnnotations annotations)
        {
            await annotations.AddAttributes(this);

            AddAttribute("data-trydotnet-order", Order.ToString("F0"));

            AddAttribute("data-trydotnet-mode", annotations.Editable ? "editor" : "include");

            if (annotations.DestinationFile != null)
            {
                AddAttributeIfNotExist(
                    "data-trydotnet-file-name",
                    Path.GetFileName(annotations.DestinationFile.Value));
            }

            if (annotations.Hidden)
            {
                AddAttribute("data-trydotnet-visibility", "hidden");
            }

            AddAttributeIfNotNull("data-trydotnet-region", annotations.Region);
            AddAttributeIfNotNull("data-trydotnet-session-id", annotations.Session);
            AddAttribute("class", $"language-{annotations.NormalizedLanguage}");
        }

        public void RenderTo(
            HtmlRenderer renderer,
            bool inlineControls,
            bool enablePreviewFeatures)
        {
            if (!(Annotations is CodeBlockAnnotations codeBlockAnnotations))
            {
                return;
            }

            var height = $"{GetEditorHeightInEm(Lines)}em";

            if (codeBlockAnnotations.Editable)
            {
                renderer
                    .WriteLine(inlineControls
                                   ? @"<div class=""code-container-inline"">"
                                   : @"<div class=""code-container"">");
            }

            var htmlStyle = codeBlockAnnotations.Editable
                                ? new EditablePreHtmlStyle(height)
                                : codeBlockAnnotations.Hidden
                                    ? new HiddenPreHtmlStyle() as HtmlStyleAttribute
                                    : new EmptyHtmlStyle();

            renderer
                .WriteLineIf(codeBlockAnnotations.Editable, @"<div class=""editor-panel"">")
                .WriteLine(codeBlockAnnotations.Editable
                               ? $@"<pre {htmlStyle} height=""{height}"" width=""100%"">"
                               : $@"<pre {htmlStyle}>")
                .Write("<code")
                .WriteAttributes(this)
                .Write(">")
                .WriteLeafRawLines(this, true, true)
                .Write(@"</code>")
                .WriteLine(@"</pre>")
                .WriteLineIf(codeBlockAnnotations.Editable, @"</div >");

            if (inlineControls && codeBlockAnnotations.Editable)
            {
                renderer
                    .WriteLine(
                        $@"<button class=""run"" data-trydotnet-mode=""run"" data-trydotnet-session-id=""{codeBlockAnnotations.Session}"" data-trydotnet-run-args=""{codeBlockAnnotations.RunArgs.HtmlAttributeEncode()}"">{SvgResources.RunButtonSvg}</button>");

                renderer
                    .WriteLine(enablePreviewFeatures
                                   ? $@"<div class=""output-panel-inline collapsed"" data-trydotnet-mode=""runResult"" data-trydotnet-output-type=""terminal"" data-trydotnet-session-id=""{Annotations.Session}""></div>"
                                   : $@"<div class=""output-panel-inline collapsed size-to-content"" data-trydotnet-mode=""runResult"" data-trydotnet-session-id=""{Annotations.Session}""></div>");
            }

            if (codeBlockAnnotations.Editable)
            {
                renderer.WriteLine("</div>");
            }

            int GetEditorHeightInEm(StringLineGroup text)
            {
                var size = text.ToString().Split('\n').Length + 6;
                return Math.Max(8, size);
            }
        }

        public string SourceCode
        {
            get => _sourceCode;

            set
            {
                _sourceCode = value ?? "";
                Lines = new StringLineGroup(_sourceCode);
            }
        }

        public void AddAttribute(string key, string value)
        {
            this.GetAttributes().AddProperty(key, value);
        }

        public void AddAttributeIfNotExist(string key, string value)
        {
            this.GetAttributes().AddPropertyIfNotExist(key, value);
        }

        internal void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}