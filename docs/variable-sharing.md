# Variable sharing

.NET Interactive enables you to write code in multiple languages within a single notebook and in order to take advantage of those languages' different strengths, you might find it useful to share data between them. By default, .NET Interactive provides [subkernels](kernels-overview.md) for three different languages within the same process. You can share variables between .NET subkernels using the `#!share` magic command.

<img src="https://user-images.githubusercontent.com/547415/82468160-55d48c00-9a77-11ea-89f6-6b167d4cf8a2.png" width="40%">

Variables are shared by reference for reference types. A consequence of this is that if you share a mutable object, changes to its state will be visible across subkernels:

<img src="https://user-images.githubusercontent.com/547415/82737074-009cb280-9ce3-11ea-82a2-8ef509cb7122.png" width="40%">

## Direct data entry with `#!value`

It's common to have text that you'd like to use in a notebook. It might be JSON, CSV, XML, or some other format. It might be in a file, in your clipboard, or on the web. The `#!value` magic command is available to make it as easy as possible to get that text into a variable in your notebook. An important thing to know is that `#!value` is an alias to a  subkernel designed just to hold values. This means that once you store something in it, you can access it from another subkernel using `#!share`.

There are a number of ways to use it. The simplest is to paste some text into the cell. The text will be stored as a string, but unlike using a `string` literal in C#, F#, or PowerShell, there's no need to escape anything.

<img src="https://user-images.githubusercontent.com/547415/89252742-81273b80-d5cf-11ea-8769-6d51eaa0669f.png" width="40%">


Optionally, you can display the value in the notebook when you submit a value, using the mime type of your choice. This accomplishes a few things. If your notebook frontend knows how to display that mime type, you can see it appropriately formatted:

<img src="https://user-images.githubusercontent.com/547415/89252758-8ab0a380-d5cf-11ea-9873-78d7060f8157.png" width="40%">

This also effectively stores the value in your `.ipynb` file, something that would not otherwise happen.

## Limitations

Variable sharing has some limitations to be aware of. When sharing a variable with a subkernel where its compilation requirements aren't met, for example due to a missing `using` (C#) or `open` (F#) declaration, a custom type defined in the notebook, or a missing assembly reference, `#!share` will fail. This limitation may be lifted in the future but for now, if you want to share variables of types that aren't imported by default, you will have to explicitly run the necessary import code in the destination kernel.

<img src="https://user-images.githubusercontent.com/547415/88083515-0292be80-cb38-11ea-953d-a392f9dda784.png" width="40%">

