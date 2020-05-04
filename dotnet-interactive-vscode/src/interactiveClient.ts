import { Observable } from 'rxjs';
import { EventEnvelope } from './events';
import { ClientTransport } from './interfaces';

export class InteractiveClient {

    constructor(readonly clientTransport: ClientTransport) {
    }

    completion(language: string, code: string, line: number, character: number): Observable<EventEnvelope> {
        let position = 0;
        let currentLine = 0;
        let currentCharacter = 0;
        for (; position < code.length; position++) {
            if (currentLine === line && currentCharacter === character) {
                break;
            }

            switch (code[position]) {
                case '\n':
                    currentLine++;
                    currentCharacter = 0;
                    break;
                default:
                    currentCharacter++;
                    break;
            }
        }
        let command = {
            code: code,
            cursorPosition: position,
        };

        return this.clientTransport.submitCommand('RequestCompletion', command, language);
    }

    hover(language: string, code: string, line: number, character: number): Observable<EventEnvelope> {
        let command = {
            code: code,
            position: {
                line: line,
                character: character,
            }
        };
        return this.clientTransport.submitCommand('RequestHoverText', command, language);
    }

    submitCode(language: string, code: string): Observable<EventEnvelope> {
        let command = {
            code: code,
            submissionType: 0,
        };
        return this.clientTransport.submitCommand('SubmitCode', command, language);
    }
}
