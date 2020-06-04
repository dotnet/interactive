# Variable sharing

Because .NET Interactive enables you to write code in multiple languages within a single notebook, it can be useful to share state between the different languages. There are three subkernels that run on .NET Core in the same process in the typical configuration of `dotnet-interactive`. You can share variables between them using the `#!share` magic command.

<img src="https://user-images.githubusercontent.com/547415/82468160-55d48c00-9a77-11ea-89f6-6b167d4cf8a2.png" width="40%">

_**Note**: When sharing a variable with a kernel where its compilation requirements aren't met, for example due to a missing `using` (C#) or `open` (F#) declaration or a missing assembly reference, this operation will fail. This limitation may be lifted in the future but for now, if you want to share variables of types that aren't imported by default, you will have to explicitly run the necessary import code in the destination kernel._

Variables are shared by reference for reference types. A consequence of this is that if you share a mutable object, changes to its state will be visible across kernels:

<img src="https://user-images.githubusercontent.com/547415/82737074-009cb280-9ce3-11ea-82a2-8ef509cb7122.png" width="40%">
