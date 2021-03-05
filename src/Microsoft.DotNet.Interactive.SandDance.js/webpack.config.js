const path = require('path');
const version = require('./package.json').version;


// Custom webpack rules


const rules = [
    { test: /\.ts$/, loader: 'ts-loader' },
    {
        test: /\.css$/i,
        use: [
            { loader: 'style-loader', options: { injectType: 'singletonStyleTag' } },
            "css-loader"
        ]
    }
];

// Packages that shouldn't be bundled but loaded at runtime
const externals = ['@jupyter-widgets/base'];

const resolve = {
    // Add '.ts' and '.tsx' as resolvable extensions.
    extensions: [".webpack.js", ".web.js", ".ts", ".tsx", ".js", ".jsx"]
};

module.exports = [
    {
        mode: 'production',
        entry: './src/index.ts',
        output: {
            filename: 'sanddanceapi.js',
            path: path.resolve(__dirname, 'dist'),
            libraryTarget: 'umd',
            library: 'sandDance',
            globalObject: 'this'
        },
        module: {
            rules: rules
        },
        externals,
        resolve,
        optimization: {
            minimize: true
        }
    }
];