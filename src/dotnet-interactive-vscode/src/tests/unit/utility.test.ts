import { expect } from 'chai';

import { getMimeType } from '../../utilities';

describe('Utility tests', () => {
    it('Detect mime type from `any`', () => {
        function verify(mimeType: string, value: any) {
            let detectedMimeType = getMimeType(value);
            expect(detectedMimeType).to.equal(mimeType);
        }

        verify('text/plain', 'text'); // string
        verify('text/plain', 5); // number
        verify('application/json', { value: 5 });
    });
});
