# Variable sharing

.NET Interactive enables you to write code in multiple languages within a single notebook and in order to take advantage of those languages' different strengths, you might find it useful to share data between them. By default, .NET Interactive provides [subkernels](kernels-overview.md) for three different languages within the same process. You can share variables between .NET subkernels using the `#!share` magic command.

<img src="https://user-images.githubusercontent.com/547415/82468160-55d48c00-9a77-11ea-89f6-6b167d4cf8a2.png" width="40%">

Variables are shared by reference for reference types. A consequence of this is that if you share a mutable object, changes to its state will be visible across subkernels:

<img src="https://user-images.githubusercontent.com/547415/82737074-009cb280-9ce3-11ea-82a2-8ef509cb7122.png" width="40%">

## Direct data entry with `#!value`



## Limitations

Variable sharing has some limitations to be aware of. When sharing a variable with a subkernel where its compilation requirements aren't met, for example due to a missing `using` (C#) or `open` (F#) declaration, a custom type defined in the notebook, or a missing assembly reference, `#!share` will fail. This limitation may be lifted in the future but for now, if you want to share variables of types that aren't imported by default, you will have to explicitly run the necessary import code in the destination kernel.

<img src="https://user-images.githubusercontent.com/547415/88083515-0292be80-cb38-11ea-953d-a392f9dda784.png" width="40%">

