# Multi-language notebooks

With .NET Interactive you can create notebooks that use different languages together. A notebook has a default language. Any cell whose language isn't set specifically will use the default language. In JupyterLab, the default language is chosen when you create a new notebook:

<img src="https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width="70%">

When a notebook is opened in JupyterLab, its default language is displayed in the upper right corner. In this example, the default language is C#:

<img src="https://user-images.githubusercontent.com/547415/81724518-59459300-9439-11ea-8938-ce152640cf1a.png" width="70%">

You can specify the language for a cell using one of a number of [magic commands](./magic-commands.md). For example, though the default language of the following notebook is F#, it's possible to run code in C# using the `#!csharp` magic command and in PowerShell using the `#!pwsh` magic command:

<img src="https://user-images.githubusercontent.com/547415/81730391-4f745d80-9442-11ea-9f17-244471f6af83.png" width="70%">

