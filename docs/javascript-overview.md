# Using JavaScript in Polyglot Notebooks

JavaScript is one of the languages supported by default in Polyglot Notebooks. JavaScript is widely used for visualization in notebooks since the most popular notebook technologies are browser-based. While many libraries are available in languages such as Python for plotting and visualization, these are usually wrappers around JavaScript libraries. For this reason, inclusion of JavaScript and the ability to share data easily from other languages makes it an appealing option to write visualization code in JavaScript directly.

## Declaring variables

The recommended way to declare JavaScript variables in a notebook differs from the way it's usually done elsewhere.

_**TL;DR**_ Declare your JavaScript variables without using a keyword such as `let`, `const`, or `var`, like this:

```javascript
x = 123;
```

So why is this recommended?

As with other languages that weren't designed for interactive programming, using JavaScript in a notebook has a few special quirks. The difference that people most frequently encounter has to do with how to declare variables.

In JavaScript, variables can be declared in a number of ways, including `let`, `var`, and `const`, as well as without a keyword.

```javascript
let declaredWithLet = 1;
var declaredWithVar = 2;
const declaredWithConst = 3;
declaredWithoutKeyword = 4;
```

Since JavaScript variables are function-scoped, the three keyword-based approaches above will declare variables that can't be referenced outside of the function where they were declared. But the fourth example, declared without a keyword, will work. The following code shows this behavior:

```javascript
const doSomething = async () => {
    let declaredWithLet = 1;
    var declaredWithVar = 2;
    const declaredWithConst = 3;
    declaredWithoutKeyword = 4;
}

await doSomething();

try { console.log(declaredWithLet); }          catch (e) { console.log(e.toString()); }
try { console.log(declaredWithVar); }          catch (e) { console.log(e.toString()); }
try { console.log(declaredWithConst); }        catch (e) { console.log(e.toString()); }
try { console.log(declaredWithoutKeyword); }   catch (e) { console.log(e.toString()); }
```

This code produces the following output: 

```console
ReferenceError: declaredWithLet is not defined
ReferenceError: declaredWithVar is not defined
ReferenceError: declaredWithConst is not defined
4
```

So why does the fourth example work? By not using the `let`, `const`, or `var` keywords with your variable declaration, you're enabling JavaScript variable hoisting to add the variable to the top-level scope. When running in a browser (including the notebook webview), this means the variable will be added to `window`. For this reason, the final line of the above example is equivalent to the following:

```javascript
console.log(window.declaredWithoutKeyword);
```

What does this have to do with Polyglot Notebooks?

The Polyglot Notebooks JavaScript kernel executes your code submissions within an async arrow function, just like the above example:

```javascript
const doSomething = async () => {
    // Your code here
}

await doSomething();
```

This means that the JavaScript code in each cell is isolated from the others by the same function scoping mechanism. The only way to make variables declared in one cell visible in others is to allow them to be hoisted to the `window` scope, by avoiding the use of the `let`, `var`, and `const` keywords.

## Return values

In C# Script, F#, Python, and a number of other languages, the following is equivalent to a `return` statement:

```csharp
123
```

This is sometimes called a trailing expression and is a feature of many languages. However, it is not supported in JavaScript. If you would like to return a value from a JavaScript cell, you need to write this:

```javascript
return 123;
```

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

```csharp
#!set --value @javascript:array --name array

array
```

When you run this code in Polyglot Notebooks, you can see that the `array` variable has been copied to the JavaScript kernel.

<img width="509" alt="image" src="https://github.com/dotnet/interactive/assets/547415/acecdcf8-5597-4258-a1d3-5cf10c3e54d8">

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

One notable detail here is that the type of the new C# variable has changed from the original declaration. It was originally declared as `int[]`, but after sharing the array back from JavaScript, the C# kernel has a variable called `array` which is of type `JsonDocument`. Because the JavaScript kernel runs in a different process from the .NET subkernels, sharing happens via JSON serialization. Complex types shared from JavaScript to .NET are sent as JSON and deserialized using `System.Text.Json`. There are a few simple types that will be shared as their intuitive .NET counterparts. 

* JavaScript numbers are shared as `System.Decimal`
* JavaScript strings are shared as `System.String`
* JavaScript booleans are shared as `System.Boolean`

You can read more about variable sharing [here](variable-sharing.md).
