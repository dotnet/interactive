using System;
using System.Text;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class RValueAdapterCommTarget : IValueAdapterCommDefinition
    {
        private const string _commTargetDefinition = @"
library(IRkernel);
library(jsonlite);
.value_adapter_comm_env <- new.env();

.value_adapter_comm_env$value_adapter_connect_to_comm <- function(comm, data) {
    comm$on_msg(function(msg) {
        assign('debug.onmsg', msg, globalenv());
        if (msg$type == 'request') {
            command <- msg$command;

            if (command == 'setVariable') {
                varInfo <- msg$arguments;
                varName <- varInfo$name;
                resultVal <- varInfo$value;

                if (varInfo$type == 'application/table-schema+json') {
                    resultVal <- data.frame(varInfo$value$Data);
                } else if (varInfo$type == 'application/json') {
                    resultVal <- fromJSON(varInfo$value);
                };
                
                assign(varName, resultVal, globalenv());
                response <- list(
                    type = 'response',
                    command=command, 
                    success=TRUE, 
                    body=list(
                        name=varName, 
                        type=typeof(resultVal)
                    )
                );
                comm$send(response);
            }
        }
    });
    
    comm$send(list(type='event', event='initialized'));
};

";

        public string GetTargetDefinition(string targetName)
        {
            StringBuilder builder = new StringBuilder(_commTargetDefinition);
            builder.Append($"comm_manager()$register_target('{targetName}', .value_adapter_comm_env$value_adapter_connect_to_comm);");
            builder.Replace(Environment.NewLine, "");
            builder.Replace(" ", ""); // remove whitespaces as well in R
            builder.Replace("elseif", "else if"); // but preserve the else if conditions. TODO: change to some minification formatter
            return builder.ToString();
        }
    }
}
