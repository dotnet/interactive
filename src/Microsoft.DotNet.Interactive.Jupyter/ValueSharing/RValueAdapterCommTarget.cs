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
    comm$on_msg(function(.msg) {
        if (.msg$type == 'request') {
            .command <- .msg$command;

            .response <- list(
                    type = 'response',
                    command=.command, 
                    success=FALSE, 
                    body=list()
                    );
            
            if (.command == 'setVariable') {
                varInfo <- .msg$arguments;
                varName <- varInfo$name;
                resultVal <- varInfo$value;

                if (varInfo$type == 'application/table-schema+json') {
                    resultVal <- data.frame(varInfo$value$Data);
                } else if (varInfo$type == 'application/json') {
                    resultVal <- fromJSON(varInfo$value);
                };
                
                assign(varName, resultVal, globalenv());
                .response <- list(
                    type = 'response',
                    command=.command, 
                    success=TRUE, 
                    body=list(
                        name=varName, 
                        type=typeof(resultVal)
                    )
                );
                
            } else if (.command == 'getVariable') {
                varInfo <- .msg$arguments;
                varName <- varInfo$name;
                
                if (!is.na(varName) && varName != '' && exists(varName)) {
                    rawValue = get(varName);
                    varType = if (is.data.frame(rawValue)) 'application/table-schema+json' else varInfo$type;
                    .response <- list(
                        type = 'response',
                        command=.command, 
                        success=TRUE, 
                        body=list(
                            name=varName, 
                            value=rawValue,
                            type=varType
                        )
                    );  
                };
            } else if (.command == 'variables') {
                allVar <- ls(all=TRUE, globalenv());
                variableList <- list();
                for (var in allVar) {
                    if (!startsWith(var, '.')) {
                        type <- toString(typeof(get(var)));
                        variableList <- append(variableList, list(list(name=var, type=type)));
                    };
                };
                .response <- list(
                        type = 'response',
                        command=.command, 
                        success=TRUE, 
                        body=list(
                            variables=variableList
                        )
                    );  
            };
            comm$send(.response);
        }
    });
    
    comm$send(list(type='event', event='initialized'));
};

";

        public string GetTargetDefinition(string targetName)
        {
            StringBuilder builder = new StringBuilder(_commTargetDefinition);
            builder.Append($"comm_manager()$register_target('{targetName}', .value_adapter_comm_env$value_adapter_connect_to_comm);");

            return builder.ToString();
        }
    }
}
