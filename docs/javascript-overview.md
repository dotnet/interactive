# Using JavaScript in Polyglot Notebooks

JavaScript is one of the languages that's supported by default in Polyglot Notebooks. JavaScript is widely used for visualizations in notebooks since the most popular notebook technologies have long been browser-based. While wrapper libraries for plotting and visualization libraries have become very popular, inclusion of JavaScript and the ability to share data easily from other languages makes it an appealing option to write visualization code directly in the original language these libraries.

## Loading dependencies

The first step to using a library is to load it into your notebook session. A couple of approaches are supported today.

### Using `import`

If the library you'd like to use is an ES module, you can import it using the `import` function. In the following example, [D3.js](https://d3js.org/) is loaded using `import` and stored in a variable called `d3`. 

```javascript
d3 = await import("https://cdn.jsdelivr.net/npm/d3@7/+esm");
```

Note that using `let`, `const`, or `var` here would prevent the variable from being hoisted into the global scope, which would prevent it from being visible to JavaScript code in other cells.

You can see a notebook with a working example [here](../samples/notebooks/javascript/D3.js%20with%20import.ipynb).

### Using RequireJS

You can also use [RequireJS](https://requirejs.org/) to load dependencies. If the library you'd like to use isn't available as an ES module, this can be a good alternative.

In this example, we're configuring RequireJS to load the [D3.js](https://d3js.org/) library from a CDN.

```javascript
configuredRequire = (require.config({
    paths: {
        d3: 'https://cdn.jsdelivr.net/npm/d3@7.4.4/dist/d3.min'
    },
}) || require);
```

Afterwards, we can use the `configuredRequire` function to call the D3.js API.

```javascript
configuredRequire(['d3'], d3 => {
    // Call d3 here.
});
```

When the module you want to load has its own dependencies, you can load them as well. Here's an example that loads [Plotly](https://plotly.com/javascript/) along with its dependencies, [jQuery](https://jquery.com/) and [D3.js](https://d3js.org/).

```javascript
configuredRequire = (require.config({
    paths: {
        d3: 'https://cdn.jsdelivr.net/npm/d3@7.4.4/dist/d3.min',
        jquery: 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min',
        plotly: 'https://cdn.plot.ly/plotly-2.14.0.min'
    },

    shim: {
        plotly: {
            deps: ['d3', 'jquery'],
            exports: 'plotly'
        }
    }
}) || require);
```

If you'd like to try it out, there's a notebook with a working example using [here](../samples/notebooks/javascript/Plotly%20with%20RequireJS.ipynb).


## Sharing data

Many polyglot notebook workflows use languages such as C#, F#, and SQL to gather and prepare data. Like other .NET Interactive subkernels, the JavaScript kernel allows you to share variables, so you can use JavaScript to plot and visualize your data. Variable sharing in JavaScript works similarly to other languages, using the `#!share` magic command. Here's a simple example, declaring an array variable in C# and then accessing it from JavaScript:

```csharp
var array = new[] { 1, 2, 3 };
```

```javascript
#!share --from csharp array

return array;
```

When you run this code in Polyglot Notebooks, you can see that the `array` variable has been copied to the JavaScript kernel.

<img width="509" alt="image" src="https://user-images.githubusercontent.com/547415/211658845-ac8563ec-accc-462d-bf24-23bef205a0c7.png">

You can also share back in the other direction. Let's modify the JavaScript array and share it back to C#.

```javascript
array.push(4);
```

```csharp
#!share --from javascript array

array
```

When you run this code, you can see that the original C# variable gets overwritten. 

<img width="503" alt="image" src="https://user-images.githubusercontent.com/547415/211661641-057e29fc-8048-4910-983f-14d8380dca8b.png">

One notable detail is that the type of the new C# variable has changed from the original declaration. It was originally declared as `int[]`, but after sharing the array back from JavaScript, the C# kernel has a variable called `array` which is of type `JsonDocument`. Because the JavaScript kernel runs in a different process from the .NET subkernels, sharing happens via JSON serialization. Complex types shared from JavaScript to .NET are sent as JSON and deserialized using `System.Text.Json`. There are a few simple types that will be shared as their intuitive .NET counterparts. 

* JavaScript numbers are shared as `System.Decimal`
* JavaScript strings are shared as `System.String`
* JavaScript booleans are shared as `System.Boolean`

You can read more about variable sharing [here](variable-sharing.md).
