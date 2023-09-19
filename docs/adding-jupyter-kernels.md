# Adding Jupyter Kernels

.NET interactive kernel already supports connecting to any Jupyter kernel as a sub kernel. The kernel can be selected by specifying the kernel spec in the jupyter connect command as shown below.

```
#!connect jupyter --kernel-name pythonkernel --conda-env base --kernel-spec python3
```

This will enable kernel launch, code execution and code completion if provided by the kernel. However, by default Jupyter messaging protocol does not have support for variable sharing so the connected kernel will not have variable sharing enabled. 

In .NET interactive kernel variable sharing is enabled for IPython and IR kernels by using the `comms` message support in [Jupyter messaging protocol](https://jupyter-client.readthedocs.io/en/stable/messaging.html#custom-messages). As long as a Jupyter kernel supports registering comms target and handling comms messages, variable sharing can be enabled for these kernels. 

### To enable variable sharing for Jupyter sub-kernel, follow the steps below:

1. Ensure that the kernel to be added supports handling [`comms`](https://jupyter-client.readthedocs.io/en/stable/messaging.html#custom-messages) messages. If not, variable sharing cannot be enabled. 
2. If intended kernel supports `comms` messages, create a language specific script for it under the [LanguageHandlers](https://github.com/dotnet/interactive/tree/main/src/Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers) folder in the .NET interactive kernel repo.
3. This script is sent to the kernel on kernel launch should create a comm message handler and a comm target named: `dotnet_coe_handler_comm`, the script is responsible for registering the target and the comm handlers. Take a look at the [python script](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers/python/coe_comm_handler.py) here. The expected schema of messages to be send back in the comm_msg data field is

```json
{
    "data": {
        "type": "object", 
        "description": "comm message data",
        "properties": {
            "commandOrEvent": {
                "type": "string"
                "description": "serialized json string of either KernelEventEnvelope or KernelCommandEnvelope"
            }, 
            "type": {
                "type": "string"
                "description": "`command` or `event`"
            }
        }
      }
}
```

`commandOrEvent` is a serialized json string reflecting either the [KernelEventEnvelope](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive/Connection/KernelEventEnvelope.cs) or the [KernelCommandEnvelope](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive/Connection/KernelCommandEnvelope.cs). The type field is used to indicate whether the message is an event or a command.

On launch, the .NET interactive kernel will run the script on the kernel to set up target and then try to send a `comm_open` message to the kernel to establish a comm connection. 

Variable sharing will only be enabled when the comm channel sends a ACK back with a `kernelReady` message and handles the send/request/request_value messages. 

### Testing your integration

Test your integration be adding the language kernel to the Http and ZMQ Test data in the following tests. The tests should not need to be modified, only your input parameters should be added. 

- [JupyterKernelCommandTests](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Jupyter.Tests/JupyterKernelCommandTests.cs)
- [JupyterKernelVariableSharingTests](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Jupyter.Tests/JupyterKernelVariableSharingTests.cs)

On first run, record the kernel message output by turning on [RECORD_FOR_PLAYBACK](https://github.com/dotnet/interactive/blob/f0979a531519000249e4b120b8a0adf2f2e9fb6d/src/Microsoft.DotNet.Interactive.Jupyter.Tests/JupyterKernelTestBase.cs#L26C1-L27C1). 
This will allow tests to be able to use the recorded output for playback in the build agents. This is only really needed if there is change in the kernel handling logic in the core .NET app as the scripts are not run in the build agent. 

In addition, ensure the scripts are testable on their own and correctly return the comm messages. Check out the python and R tests to get a list of scenarios and formatting that needs to be supported by the scripts. 

- [Python script tests](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Jupyter.Tests/LanguageHandlerTests/tests_python_coe_comm_handler.py) 
- [R script tests](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Jupyter.Tests/LanguageHandlerTests/tests_r_coe_comm_handler.r)
