const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const SpeedMeasurePlugin = require("speed-measure-webpack-plugin");
const smp = new SpeedMeasurePlugin();

module.exports = smp.wrap({
  entry: path.join(__dirname, "src/js/index.js"),

  output: {
    path: path.resolve(__dirname, "dist"),
    filename: "[name].[contenthash].js",
  },

  resolve: {
    extensions: ['.tsx', '.ts', '.js', '.css'],
  },

  plugins: [
    new HtmlWebpackPlugin({
      template: path.join(__dirname, "src/html/index.html"),
    }),
  ],

  devServer: {
    // this enabled hot module replacement of modules so when you make a change in a javascript or css file the change will reflect on the browser
    hot: true,
    // port that the webpack-dev-server runs on; must match the later configuration where ASP.NET Core knows where to execute
    port: 8400,
  },

  module: {
    rules: [{
      test: /\.js$/,
      exclude: /(node_modules)/,
      use: {
        loader: 'babel-loader',
        options: {
          presets: ['@babel/preset-react'],
        }
      }
    }, {
      test: /\.tsx?$/,
      use: 'ts-loader',
      exclude: /node_modules/,
    }, {
      test: /\.css$/,
      use: [{
        loader: 'style-loader',
        options: {
          injectType: "styleTag"
        }
      }, "css-loader"]
    }
    ]
  },

  mode: 'production',
})
;
