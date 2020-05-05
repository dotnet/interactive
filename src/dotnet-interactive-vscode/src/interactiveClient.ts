import { Observable } from 'rxjs';
import { ClientTransport } from './interfaces';
import { KernelEventEnvelope, RequestCompletion, RequestCompletionType, RequestHoverText, RequestHoverTextType, SubmissionType, SubmitCode, SubmitCodeType } from './contracts';

export class InteractiveClient {

    constructor(readonly clientTransport: ClientTransport) {
    }

    completion(language: string, code: string, line: number, character: number): Observable<KernelEventEnvelope> {
        let command: RequestCompletion = {
            code: code,
            position: {
                line,
                character
            },
        };

        return this.clientTransport.submitCommand(RequestCompletionType, command, language);
    }

    hover(language: string, code: string, line: number, character: number): Observable<KernelEventEnvelope> {
        let command: RequestHoverText = {
            code: code,
            position: {
                line: line,
                character: character,
            }
        };
        return this.clientTransport.submitCommand(RequestHoverTextType, command, language);
    }

    submitCode(language: string, code: string): Observable<KernelEventEnvelope> {
        let command: SubmitCode = {
            code: code,
            submissionType: SubmissionType.Run,
        };
        return this.clientTransport.submitCommand(SubmitCodeType, command, language);
    }
}
