// From https://github.com/mermaidjs/mermaid-webpack-demo/

const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TerserPlugin = require('terser-webpack-plugin');
const path = require('path');

const config = {
  target: 'web',
  entry: {
    'mermaidapi': './mermaidapi.js'
  },
  externals: 'fs', // in order to make mermaid work
  output: {
    library: 'mermaid',
    libraryTarget: 'umd',
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].js',
    library: '[name]',
    libraryTarget: 'umd',
  },
  module: {
    rules: [
      {
        parser: {
          amd: false
        },
        include: /node_modules\/lodash\// // https://github.com/lodash/lodash/issues/3052
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader'
        }
      },
      {
        test: /\.css$/,
        use: [
          MiniCssExtractPlugin.loader,
          "css-loader"
        ]
      }
    ]
  },
  plugins: [
    new MiniCssExtractPlugin({
      // Options similar to the same options in webpackOptions.output
      // both options are optional
      filename: "[name].css",
      chunkFilename: "[id].css"
    })
  ],
  optimization: {
    minimize: false,
    minimizer: [new TerserPlugin()],
  },
}

module.exports = [config]