// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;
using Microsoft.DotNet.Interactive.PowerShell.Host.Progress;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    public partial class PSKernelHostUserInterface
    {
        // Note: the progress handling code is from the PowerShell ConsoleHost implementation,
        // with the necessary refactoring and updates for it to work with the PowerShell kernel.

        // Time in milliseconds to refresh the rendering of a progress bar.
        private const int UpdateTimerThreshold = 200;
        private const int ToRender = 1;
        private const int ToNotRender = 0;

        private Timer _progPaneUpdateTimer = null;
        private ProgressPane _progPane = null;
        private PendingProgress _pendingProgress = null;
        private int progPaneUpdateFlag = ToNotRender;

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            lock (_instanceLock)
            {
                if (_pendingProgress == null)
                {
                    Debug.Assert(_progPane == null, "If there is no data struct, there shouldn't be a pane, either.");
                    _pendingProgress = new PendingProgress();
                }

                _pendingProgress.Update(sourceId, record);

                if (_progPane == null)
                {
                    // This is the first time we've received a progress record, so
                    //  - create a progress pane,
                    //  - set up a update flag
                    //  - create a timer for updating the flag
                    _progPane = new ProgressPane(this);

                    if (_progPaneUpdateTimer == null)
                    {
                        // Show a progress pane at the first time we've received a progress record
                        progPaneUpdateFlag = ToRender;

                        // The timer will be auto restarted every 'UpdateTimerThreshold' ms
                        _progPaneUpdateTimer = new Timer(new TimerCallback(ProgressPaneUpdateTimerElapsed), null, UpdateTimerThreshold, UpdateTimerThreshold);
                    }
                }

                if (Interlocked.CompareExchange(ref progPaneUpdateFlag, ToNotRender, ToRender) == ToRender ||
                    record.RecordType == ProgressRecordType.Completed)
                {
                    // Update the progress pane only when the timer set up the update flag or WriteProgress is completed.
                    // As a result, we do not block WriteProgress and whole script and eliminate unnecessary console locks and updates.
                    _progPane.Show(_pendingProgress);
                }
            }
        }

        /// <summary>
        /// Called at the end of a prompt loop to take down any progress display that might have appeared and purge any
        /// outstanding progress activity state.
        /// </summary>
        internal void ResetProgress()
        {
            // If we have multiple runspaces on the host then any finished pipeline in any runspace will lead to call 'ResetProgress'
            // so we need the lock.
            lock (_instanceLock)
            {
                if (_progPaneUpdateTimer != null)
                {
                    // Stop update a progress pane and destroy the timer.
                    _progPaneUpdateTimer.Dispose();
                    _progPaneUpdateTimer = null;
                }

                // We don't set 'progPaneUpdateFlag = ToNotRender' here, because:
                // 1. According to MSDN, the timer callback can occur after the Dispose() method has been called.
                //    So we cannot guarantee the flag is truly set to 'ToNotRender'.
                // 2. When creating a new timer in 'HandleIncomingProgressRecord', we will set the flag to 'ToRender' anyways.
                if (_progPane != null)
                {
                    _progPane.Hide();
                    _progPane = null;
                }

                _pendingProgress = null;
            }
        }

        private void ProgressPaneUpdateTimerElapsed(object sender)
        {
            Interlocked.CompareExchange(ref progPaneUpdateFlag, ToRender, ToNotRender);
        }
    }
}
