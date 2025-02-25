const fs = require('fs');
const path = require('path');

const outputDir = path.resolve(__dirname, '../artifacts/TestResults/Release');

if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}