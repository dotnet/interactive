# `PocketView`

`PocketView` is an API for concisely writing HTML, in the terminology of HTML, using C# code. Just like the `HTML` method, it returns an object that implements `IHtmlContent`, so the output will be assumed to be valid HTML and rendered into your notebook. Here's an example:

![image](https://user-images.githubusercontent.com/547415/82271031-7e4f6f80-992b-11ea-9f25-7e34a96b0e14.png)

Let's consider what's happening in this code.

* We're calling a `span` method and passing in the results of three other methods: `img`, `HTML`, and `a`.
* The `img` method is being invoked with an indexer that has a couple of named parameters being passed to it, `src` and `style`.
* The `HTML` method has a string containing the `&nbsp;` HTML element.
* The `a` method has another indexer parameter (`href`) as well as an argument, in this case a call to an `i` method.

This makes a reasonable amount of sense if you understand HTML but perhaps less sense if you consider that this is valid C# code. What is going on here? 

You can see the actual HTML that this produces by converting a `PocketView` to a string, which will be shown in plain text rather than as HTML:

![image](https://user-images.githubusercontent.com/547415/82271047-8ad3c800-992b-11ea-9218-d4d33a88fbe9.png)

Because HTML is hard to represent concisely using a statically-typed language, `PocketView` makes use of `dynamic`. This is what allows you to use arbitrary indexer names to specify HTML attributes. Each of the methods on `PocketViewTags` returns a `PocketView` as `dynamic`. Let's take a closer look at some of the things you can do with a `PocketView`.

_Note: In the code samples shown here, the `PocketView` methods can be called without a class name qualifier by running `using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags` in your notebook._

As we saw above, you can use square bracket-delimited indexers to specify HTML attributes:

![image](https://user-images.githubusercontent.com/547415/82272423-6e398f00-992f-11ea-95a8-9e1711a0443c.png)

If you pass a `string` to `PocketView`, it will be HTML encoded for you:

![image](https://user-images.githubusercontent.com/547415/82272929-dfc60d00-9930-11ea-9b3a-7df1138ed8e4.png)

This is where the `HTML` method can be useful. If you want to pass a `string` to a `PocketView` but you don't want it to be HTML encoded, you can simply wrap that string in a call to `HTML`:

![image](https://user-images.githubusercontent.com/547415/82273032-33385b00-9931-11ea-94a1-c890f2b4c653.png)

`HTML` simply captures a `string` into an instance a type implementing `IHtmlContent`. This interface is used to signal that the value should not be HTML-encoded but rather treated as valid HTML and rendered directly.

Seeing how that works, it might not come as a surprise to know that `PocketView` itself implements `IHtmlContent`.

You can pass other types of objects of into a `PocketView` as well. When you do this, they're formatted using the plain text formatter, which by default expands the object's properties.

![image](https://user-images.githubusercontent.com/547415/82273371-31bb6280-9932-11ea-9c7d-4eca542fc109.png)

Since .NET Interactive's formatter API doesn't currently support generating formatters for nested HTML fragments, `PocketView` falls back to a formatter for `text/plain` to describe the properties of the passed object.

You can also pass lists of objects to a `PocketView`. This can be useful for creating HTML from data:

![image](https://user-images.githubusercontent.com/547415/82274070-06397780-9934-11ea-9ce6-ec3ad9b75df0.png)


