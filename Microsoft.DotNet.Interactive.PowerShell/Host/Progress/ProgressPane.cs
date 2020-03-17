// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.PowerShell.Host.Progress
{
    /// <summary>
    /// ProgressPane is a class that represents the "window" in which outstanding activities for which the host has received
    /// progress updates are shown.
    ///</summary>
    internal class ProgressPane
    {
        private DisplayedValue _displayValue;
        private PSKernelHostUserInterface _ui;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="ui">
        /// An implementation of the PSHostRawUserInterface with which the pane will be shown and hidden.
        /// </param>
        internal ProgressPane(PSKernelHostUserInterface ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _displayValue = null;
        }

        /// <summary>
        /// Hides the pane by restoring the saved contents of the region of the buffer that the pane occupies.  If the pane is
        /// not showing, then does nothing.
        /// </summary>
        internal void Hide()
        {
            _displayValue?.Update(string.Empty);
        }

        /// <summary>
        /// Updates the pane with the rendering of the supplied PendingProgress, and shows it.
        /// </summary>
        /// <param name="pendingProgress">
        /// A PendingProgress instance that represents the outstanding activities that should be shown.
        /// </param>
        internal void Show(PendingProgress pendingProgress)
        {
            // In order to keep from slicing any CJK double-cell characters that might be present in the screen buffer,
            // we use the full width of the buffer.
            int maxWidth = _ui.RawUI.BufferSize.Width;
            int maxHeight = Math.Max(5, _ui.RawUI.WindowSize.Height / 3);

            List<string> contents = pendingProgress.Render(maxWidth, maxHeight, _ui);
            if (contents == null)
            {
                Hide();
                return;
            }

            for (int i = 0; i < contents.Count; i++)
            {
                int length = contents[i].Length;
                if (length < maxWidth)
                {
                    contents[i] += StringUtil.Padding(maxWidth - length);
                }
            }

            string textToRender = string.Join('\n', contents);
            if (_displayValue == null)
            {
                _displayValue = KernelInvocationContext.Current?.Display(textToRender, "text/plain");
            }
            else
            {
                _displayValue.Update(textToRender);
            }
        }
    }
}
