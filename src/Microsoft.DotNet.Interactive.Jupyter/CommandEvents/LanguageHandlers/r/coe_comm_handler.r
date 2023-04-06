# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

library(IRkernel);
library(jsonlite);

.dotnet_coe_comm_hander_env <- new.env();

.dotnet_coe_comm_hander_env$emptyEvent <- fromJSON("{}")

# events
.dotnet_coe_comm_hander_env$KernelReady <- 'KernelReady';
.dotnet_coe_comm_hander_env$CommandSucceeded <- 'CommandSucceeded';
.dotnet_coe_comm_hander_env$CommandFailed <- 'CommandFailed';
.dotnet_coe_comm_hander_env$ValueProduced <- 'ValueProduced';
.dotnet_coe_comm_hander_env$ValueInfosProduced <- 'ValueInfosProduced';

#commands
.dotnet_coe_comm_hander_env$SendValue <- 'SendValue';
.dotnet_coe_comm_hander_env$RequestValue <- 'RequestValue';
.dotnet_coe_comm_hander_env$RequestValueInfos <- 'RequestValueInfos';

.dotnet_coe_comm_hander_env$json <- function(value) {
    return (toJSON(value, auto_unbox = TRUE, null="null", force = TRUE))
}
    
.dotnet_coe_comm_hander_env$payload <- function(envelope, type) {
    payload <- list(commandOrEvent = .dotnet_coe_comm_hander_env$json(envelope), type = type);
    return (payload);
}

.dotnet_coe_comm_hander_env$eventEnvelope <- function(event, eventType, command = NA) {
    if (!is.na(command) && !is.null(command)) {
        # we don't care about routing slip here and there are some json serialization issues with R un-boxing
        # for now, let's remove it or make it empty
        command$routingSlip <- list()
    }
    envelope <- list(event=event, eventType=eventType, command=command);
    return (.dotnet_coe_comm_hander_env$payload(envelope, 'event'));
}

.dotnet_coe_comm_hander_env$is_ready <- function() {
    return (
        .dotnet_coe_comm_hander_env$eventEnvelope(
                list(kernelInfos=list()), 
                .dotnet_coe_comm_hander_env$KernelReady)
    );
}

.dotnet_coe_comm_hander_env$fail <- function(message = NA, command = NA) {
    return (
        .dotnet_coe_comm_hander_env$eventEnvelope(
                list(message=message), 
                .dotnet_coe_comm_hander_env$CommandFailed, 
                command)
    );
}

.dotnet_coe_comm_hander_env$pass <- function(command = NA) {
    return (
        .dotnet_coe_comm_hander_env$eventEnvelope(
                .dotnet_coe_comm_hander_env$emptyEvent, 
                .dotnet_coe_comm_hander_env$CommandSucceeded, 
                command)
    );
}

.dotnet_coe_comm_hander_env$get_formatted_value <- function(value, mimeType = 'application/json') {
    formattedValue = NULL
    if (is.data.frame(value)) {
        mimeType <- 'application/table-schema+json'
        formattedValue <- .dotnet_coe_comm_hander_env$json(head(value))
    } else if (mimeType == 'application/json') {
        formattedValue <- .dotnet_coe_comm_hander_env$json(value)
    }
    return (list(
        mimeType=mimeType,
        value=formattedValue
       ))
}

.dotnet_coe_comm_hander_env$handle_request_value_infos <- function(commandOrEvent) {
    variables <- ls(all=TRUE, globalenv()) # we only retrieve the global variables 
    results <- list();
    
    for (var in variables) {
        if (!startsWith(var, '.')) {
            value <- get(var);
            type <- if (is.data.frame(value)) 'data.frame' else toString(typeof(value));
            if (type != 'closure') {
                formattedValue <- .dotnet_coe_comm_hander_env$get_formatted_value(value);
                results <- append(results, list(list(name=var, formattedValue=formattedValue, typeName=type)));
            }
        };
    };
                
    
    valueInfosProduced = list(valueInfos=results)
    
    response <- .dotnet_coe_comm_hander_env$eventEnvelope(
                valueInfosProduced, 
                .dotnet_coe_comm_hander_env$ValueInfosProduced, 
                commandOrEvent)
}

.dotnet_coe_comm_hander_env$handle_request_value <- function(commandOrEvent) {
    requestValue <- commandOrEvent$command
    mimeType <- requestValue$mimeType
    name <- requestValue$name
    
    if (is.na(name) || name == '' || !exists(name)) {
        return (
            .dotnet_coe_comm_hander_env$fail(
                    sprintf('Variable "%s" not found.', name))
        )
    }
    
    rawValue <- get(name);
    mimeType <- if (is.data.frame(rawValue)) 'application/table-schema+json' else mimeType;
    formattedValue <- .dotnet_coe_comm_hander_env$get_formatted_value(rawValue, mimeType);

    valueProduced = list(
                        name=name, 
                        value=rawValue, 
                        formattedValue=formattedValue
                    )
    response <- .dotnet_coe_comm_hander_env$eventEnvelope(
                valueProduced, 
                .dotnet_coe_comm_hander_env$ValueProduced, 
                commandOrEvent)
    
    return (response)
}

.dotnet_coe_comm_hander_env$handle_send_value <- function(commandOrEvent) {
    sendValue <- commandOrEvent$command
    mimeType <- sendValue$formattedValue$mimeType
    name <- sendValue$name
    rawValue <- sendValue$formattedValue$value
    resultValue = NA
    
    if (make.names(name) != name) {
        return (
            .dotnet_coe_comm_hander_env$fail(
                    sprintf('Invalid Identifier: "%s"', name))
        )
    }
    
    if (mimeType == 'application/table-schema+json') {
        resultValue <- fromJSON(rawValue)
        resultValue <- data.frame(resultValue$data)
    } else if (mimeType == 'application/json') {
        resultValue <- fromJSON(rawValue)
    } else {
        return (
            .dotnet_coe_comm_hander_env$fail(
                        sprintf('Failed to set value for "%s". "%s" mimetype not supported.', name, mimeType))
        )
    }
    
    
    assign(name, resultValue, globalenv());
    return (.dotnet_coe_comm_hander_env$pass())
}

.dotnet_coe_comm_hander_env$handle_command <- function(commandOrEvent) {
    commandType <- commandOrEvent$commandType

    result <- .dotnet_coe_comm_hander_env$fail(
                sprintf('command "%s" not supported', commandType)
            )

    if (commandType == .dotnet_coe_comm_hander_env$SendValue) {
        result <- .dotnet_coe_comm_hander_env$handle_send_value(commandOrEvent)
    } else if (commandType == .dotnet_coe_comm_hander_env$RequestValue) {
        result <- .dotnet_coe_comm_hander_env$handle_request_value(commandOrEvent)
    } else if (commandType == .dotnet_coe_comm_hander_env$RequestValueInfos) {
        result <- .dotnet_coe_comm_hander_env$handle_request_value_infos(commandOrEvent)
    }

    return (result)
}

.dotnet_coe_comm_hander_env$handle_command_or_event <- function(msg) {
    response <- tryCatch({
            msg_type <- msg$type
            commandOrEvent <- fromJSON(msg$commandOrEvent)
        
            if (msg_type == 'command') {
                return (.dotnet_coe_comm_hander_env$handle_command(commandOrEvent))
            } 
        },
        error=function(cond) {
            return (
                .dotnet_coe_comm_hander_env$fail(
                    sprintf('failed to process comm data. %s', cond$message))
                )
        })    
    
    return(response)
}

.dotnet_coe_comm_hander_env$coe_handler_connect_to_comm <- function(comm, data) {
    comm$on_msg(function(msg) {
        # assign('.debug.onmsg', msg, globalenv());
        response <- .dotnet_coe_comm_hander_env$handle_command_or_event(msg);
        comm$send(response);  
    })

    ready <- .dotnet_coe_comm_hander_env$is_ready()
    comm$send(ready);  
    
};

if(!is.null(comm_manager())) {
    comm_manager()$register_target('dotnet_coe_handler_comm', .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm);
}
