# Multi-language notebooks

With .NET Interactive you can create notebooks that use different languages together. A notebook has a default language, and in frontends with no per-cell language picker (e.g. JupyterLab), any cell whose language isn't set will use the default language. In JupyterLab, the default language is chosen when you create a new notebook:

<img src="https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width="70%">

When a notebook is opened in JupyterLab, its default language is displayed in the upper right corner. In this example, the default language is C#:

<img src="https://user-images.githubusercontent.com/547415/81724518-59459300-9439-11ea-8938-ce152640cf1a.png" width="70%">

But every .NET Interactive notebook is capable of running multiple languages. You can specify the language for a cell using one of a number of [magic commands](./magic-commands.md). For example, though the default language of the following notebook is F#, it's possible to run code in C# using the `#!csharp` magic command and in PowerShell using the `#!pwsh` magic command:

<img src="https://user-images.githubusercontent.com/547415/81730391-4f745d80-9442-11ea-9f17-244471f6af83.png" width="70%">

If you're editing a notebook in Visual Studio Code, you can also choose the language for a submission by clicking the language selector in a cell's lower right corner. The following screen shot show a C# cell and then an F# cell. 

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211166094-036d5277-9c56-40c2-b9b4-392b7f18d603.png">

The language picker sets the language for the cell in the `.ipynb` metadata so that if the notebook is later opened in JupyterLab, the correct language will be used even though no magic command is present to specify it.

But language selection magic commands work in Visual Studio Code as well, and take precedence over the language picker selection. You can use the language selection magic commands to combine more than one language within a single cell. In the following example, you can see how the `#!fsharp` magic allows you to switch languages in a single cell and override the cell's language picker.

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/
211166072-3c0ad7a8-4885-428d-b56d-79cf63bdcd1c.png">


