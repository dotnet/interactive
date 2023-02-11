# Variable sharing

The .NET Interactive kernel enables you to write code in multiple languages within a single notebook. In order to take advantage of the different strengths of each language, you'll find it useful to share data between them. By default, .NET Interactive supports a number of different languages and most of them allow sharing using the `#!set` and `#!share` magic commands.

> *&nbsp;_The `#!share` magic command has been in place since the earliest days of .NET Interactive while `#!set` is newer and provides a superset of the capabilities of `#!share`._






## Sharing by reference

In certain specific cases, variables can be shared by reference for reference types. This comes with a number of caveats. 

* The source and destination kernels must be running in the same process.

One consequence of this is that if you share a mutable object, changes to its state will be visible across subkernels:

<img src="https://user-images.githubusercontent.com/547415/82737074-009cb280-9ce3-11ea-82a2-8ef509cb7122.png" width="40%">

## Direct data entry with `#!value`

It's common to have text that you'd like to use in a notebook. It might be JSON, CSV, XML, or some other format. It might be in a file, in your clipboard, or on the web. The `#!value` magic command is available to make it as easy as possible to get that text into a variable in your notebook. An important thing to know is that `#!value` is an alias to a  subkernel designed just to hold values. This means that once you store something in it, you can access it from another subkernel using `#!share`.

There are three ways to use `#!value` to get data into your notebook session:

### 1. From the clipboard

 The simplest way to use `#!value` is to paste some text into the cell. The text will be stored as a string, but unlike using a `string` literal in C#, F#, or PowerShell, there's no need to escape anything.

<img src="https://user-images.githubusercontent.com/547415/89252742-81273b80-d5cf-11ea-8769-6d51eaa0669f.png" width="40%">

### 2. From a file

If the data you want to read into your notebook is stored in a file, you can use `#!value` with the `--from-file` option:

<img src="https://user-images.githubusercontent.com/547415/89600459-fdf82680-d816-11ea-8ba6-1d5ec4e2a7e7.png" width="40%">

### 3. From a URL

You can pull data into your notebook from a URL as well, using the `--from-url` option. 

<img src="https://user-images.githubusercontent.com/547415/89846563-66584800-db36-11ea-8a17-57a48b45b0f1.png" width="40%">

## Specifying a MIME type

Regardless of which of these approaches you use, you can additionally choose to display the value in the notebook at the time of submission by using the `--mime-type` option. This accomplishes a few things. If your notebook frontend knows how to display that mime type, you can see it appropriately formatted:

<img src="https://user-images.githubusercontent.com/547415/89252758-8ab0a380-d5cf-11ea-9873-78d7060f8157.png" width="40%">

This also causes the value to be saved in your `.ipynb` file, something that would not otherwise happen.

## Limitations

Variable sharing has some limitations to be aware of. When sharing a variable with a subkernel where its compilation requirements aren't met, for example due to a missing `using` (C#) or `open` (F#) declaration, a custom type defined in the notebook, or a missing assembly reference, `#!share` will fail. This limitation may be lifted in the future but for now, if you want to share variables of types that aren't imported by default, you will have to explicitly run the necessary import code in the destination kernel.

<img src="https://user-images.githubusercontent.com/547415/88083515-0292be80-cb38-11ea-953d-a392f9dda784.png" width="40%">

