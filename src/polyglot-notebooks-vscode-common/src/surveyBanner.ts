// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    Disposable,
    Memento,
    NotebookDocumentChangeEvent,
    UIKind,
    env,
    workspace,
    window,
    Uri,
    l10n
} from 'vscode';
import { isStableBuild } from './vscodeUtilities';



export enum InsidersNotebookSurveyStateKeys {
    ShowBanner = 'ShowInsidersNotebookSurveyBanner',
    ExecutionCount = 'DS_InsidersNotebookExecutionCount'
}

export enum ExperimentNotebookSurveyStateKeys {
    ShowBanner = 'ShowExperimentNotebookSurveyBanner',
    ExecutionCount = 'DS_ExperimentNotebookExecutionCount'
}

enum DSSurveyLabelIndex {
    Yes,
    No
}

export enum BannerType {
    InsidersNotebookSurvey,
    ExperimentNotebookSurvey
}

export type ShowBannerWithExpiryTime = {
    /**
     * This value is not used.
     * We are only interested in the value for `expiry`.
     * This structure is based on the old data for older customers when we used PersistentState class.
     */
    data: boolean;
    /**
     * If this is value `undefined`, then prompt can be displayed.
     * If this value is `a number`, then a prompt was displayed at one point in time &
     * we need to wait for Date.now() to be greater than that number to display it again.
     */
    expiry?: number;
};

const isCodeSpace = env.uiKind === UIKind.Web;
const MillisecondsInADay = 1000 * 60 * 60 * 24;

/**
 * Puts up a survey banner after a certain number of notebook executions. The survey will only show after 10 minutes have passed to prevent it from showing up immediately.
 */

export class SurveyBanner implements Disposable {


    private disabledInCurrentSession: boolean = false;

    private bannerLabels: string[] = [
        this.translate('survey.yes', "Yes, take me to the survey"),
        this.translate('survey.no', "No")
    ];

    private readonly showBannerState = new Map<BannerType, Memento>();
    private static surveyDelay = false;
    private readonly NotebookExecutionThreshold = 250; // Cell executions before showing survey
    private onDidChangeNotebookCellExecutionStateHandler?: Disposable;
    constructor(
        private persistentState: Memento,
        private disposables: Array<Disposable>,
    ) {
        this.setPersistentState(BannerType.InsidersNotebookSurvey, InsidersNotebookSurveyStateKeys.ShowBanner);
        this.setPersistentState(BannerType.ExperimentNotebookSurvey, ExperimentNotebookSurveyStateKeys.ShowBanner);

        // Change the surveyDelay flag after 10 minutes
        const timer = setTimeout(
            () => {
                SurveyBanner.surveyDelay = true;
            },
            10 * 60 * 1000
        );
        this.disposables.push(new Disposable(() => clearTimeout(timer)));

        this.activate();
    }

    public dispose() {
        for (const disposable of this.disposables) {
            disposable.dispose();
        }
    }

    public isEnabled(type: BannerType): boolean {
        switch (type) {
            case BannerType.InsidersNotebookSurvey:
                if (!isStableBuild()) {
                    return this.isEnabledInternal(type);
                }
                break;
            case BannerType.ExperimentNotebookSurvey:
                if (isStableBuild()) {
                    return this.isEnabledInternal(type);
                }
                break;
            default:
                return false;
        }
        return false;
    }
    private isEnabledInternal(type: BannerType): boolean {
        if (env.uiKind !== UIKind.Desktop) {
            return false;
        }
        const value = this.showBannerState.get(type)?.get<ShowBannerWithExpiryTime>(InsidersNotebookSurveyStateKeys.ShowBanner);
        if (!value?.expiry) {
            return true;
        }
        return value.expiry < Date.now();
    }

    public activate() {
        this.onDidChangeNotebookCellExecutionStateHandler = workspace.onDidChangeNotebookDocument(
            this.onDidChangeNotebookCellExecutionState,
            this,
            this.disposables
        );
    }

    public async showBanner(type: BannerType): Promise<void> {
        const show = this.shouldShowBanner(type);
        this.onDidChangeNotebookCellExecutionStateHandler?.dispose();
        if (!show) {
            return;
        }
        // Disable for the current session.
        this.disabledInCurrentSession = true;
        const response = await window.showInformationMessage(this.getBannerMessage(type), ...this.bannerLabels);
        switch (response) {
            case this.bannerLabels[DSSurveyLabelIndex.Yes]: {
                await this.launchSurvey(type);
                await this.disable(DSSurveyLabelIndex.Yes, type);
                break;
            }
            // Treat clicking on x as equivalent to clicking No
            default: {
                await this.disable(DSSurveyLabelIndex.No, type);
                break;
            }
        }
    }

    private shouldShowBanner(type: BannerType) {
        if (
            isCodeSpace ||
            !this.isEnabled(type) ||
            this.disabledInCurrentSession ||
            !SurveyBanner.surveyDelay
        ) {
            return false;
        }

        const executionCount: number = this.getExecutionCount(type);

        return executionCount >= this.NotebookExecutionThreshold;
    }

    private setPersistentState(type: BannerType, val: string): void {
        this.showBannerState.set(
            type,
            this.persistentState
        );
    }

    private async launchSurvey(type: BannerType): Promise<void> {
        env.openExternal(Uri.parse(this.getSurveyLink(type)));
    }
    private async disable(answer: DSSurveyLabelIndex, type: BannerType) {
        let monthsTillNextPrompt = answer === DSSurveyLabelIndex.Yes ? 6 : 4;
        const key = type === BannerType.InsidersNotebookSurvey ? InsidersNotebookSurveyStateKeys.ShowBanner : ExperimentNotebookSurveyStateKeys.ShowBanner;
        if (monthsTillNextPrompt) {
            await this.persistentState.update(key, {
                expiry: monthsTillNextPrompt * 31 * MillisecondsInADay + Date.now(),
                data: true
            });
        }
    }

    // Handle when a cell finishes execution
    private async onDidChangeNotebookCellExecutionState(
        cellStateChange: NotebookDocumentChangeEvent
    ): Promise<void> {
        // TODO: Enure you check the notebook type is jupyter or dib
        if (!isSupportedNotebook(cellStateChange.notebook.metadata)) {
            return;
        }

        // If cell has moved to executing, update the execution count
        if (cellStateChange.cellChanges.some(cell => cell.executionSummary?.timing?.startTime)) {
            void this.updateStateAndShowBanner(
                InsidersNotebookSurveyStateKeys.ExecutionCount,
                BannerType.InsidersNotebookSurvey
            );
            void this.updateStateAndShowBanner(
                ExperimentNotebookSurveyStateKeys.ExecutionCount,
                BannerType.ExperimentNotebookSurvey
            );
        }
    }

    private getExecutionCount(type: BannerType): number {
        switch (type) {
            case BannerType.InsidersNotebookSurvey:
                return this.persistentState.get<number>(InsidersNotebookSurveyStateKeys.ExecutionCount, 0);
            case BannerType.ExperimentNotebookSurvey:
                return this.persistentState.get<number>(ExperimentNotebookSurveyStateKeys.ExecutionCount, 0);
            default:
                return -1;
        }
    }


    private async updateStateAndShowBanner(val: string, banner: BannerType) {
        const key = banner === BannerType.InsidersNotebookSurvey ? InsidersNotebookSurveyStateKeys.ExecutionCount : ExperimentNotebookSurveyStateKeys.ExecutionCount;
        const value = this.showBannerState.get(banner)?.get<number>(key, 0) || 0;
        await this.persistentState.update(key, value + 1);

        if (!this.shouldShowBanner(banner)) {
            return;
        }

        this.onDidChangeNotebookCellExecutionStateHandler?.dispose();

        void this.showBanner(banner);
    }

    private getBannerMessage(type: BannerType): string {
        switch (type) {
            case BannerType.InsidersNotebookSurvey:
            case BannerType.ExperimentNotebookSurvey:
                // TODO: LOCALIZE (message in banner for user)
                return this.translate('survey.message', "We would love to hear your feedback on the notebooks experience! Please take a few minutes to give feedback on using Polyglot Notebooks");
            default:
                return '';
        }
    }

    private getSurveyLink(type: BannerType): string {
        switch (type) {
            case BannerType.InsidersNotebookSurvey:
            case BannerType.ExperimentNotebookSurvey:
                return 'https://aka.ms/polyglotnotebooksurvey';
            default:
                return '';
        }
    }

    private translate(key: string, fallback: string): string {
        const translation = l10n.t(key);
        return translation === key ? fallback : translation;
    }
}

function isSupportedNotebook(notebookMedata: { [key: string]: any }): boolean {
    return notebookMedata && notebookMedata.polyglot_notebook;
}

