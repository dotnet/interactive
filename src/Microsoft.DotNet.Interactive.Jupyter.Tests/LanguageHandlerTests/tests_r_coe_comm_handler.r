# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Run this test using:
#   RScript tests_r_coe_comm_handler.r 

source('../../Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers/r/coe_comm_handler.r')

if (!require(testthat)) install.packages('testthat')
library(testthat)

wrap_as_json <- function(msg) {
    return (toJSON(msg, auto_unbox = TRUE, null="null"))
}

create_msg_received <- function(commandType, commandReceived = NULL) {
    command = list(targetKernelName="r", originUri=NULL, destinationUri=NULL)
    
    if (!is.null(commandReceived)) {
        command = c(command, commandReceived)
    }
    
    msg = list(
            content=list( 
                 data=list(
                     type="command", 
                     commandOrEvent=wrap_as_json(list(
                             token=19,
                             id="ccc7591568d943c9bbe7dd8254e89b0d",
                             commandType=commandType,
                             command=command,
                             routingSlip=list()
                         )
                     )
                 )
            )
        )
   return (msg)
}

create_msg_sent <- function(eventType, eventSent = fromJSON('{}'), msg_received = NULL) {
    command = NULL
    if (!is.null(msg_received)) {
        command <- fromJSON(msg_received$content$data$commandOrEvent)
    }
    
    msg = list(
            commandOrEvent=wrap_as_json(list(
                    event=eventSent, 
                    eventType=eventType, 
                    command=command
                )), 
            type="event"
          )
        
   return (wrap_as_json(msg))
}

testComm <- setRefClass(
    'testComm',
    fields = list(msg_callback = 'functionOrNULL', msg_sent = 'json'),
    methods = list(
        send = function(msg = list()) {
            msg_sent <<- wrap_as_json(msg)
        },
        on_msg = function(a_msg_callback) {
            msg_callback <<- a_msg_callback
        },
        on_close = function(a_close_callback) {
            close_callback <<- a_close_callback
        },
        handle_msg = function(msg) {
            if (!is.null(msg_callback)) {
                data <- msg$content$data
                msg_callback(data)
            }
        }
    )
)

test_that("test_can_get_kernel_ready_on_comm_open", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_sent <- create_msg_sent("KernelReady", list(kernelInfos=list()))
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_invalid_json", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received <- fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "just a string"}}}')
    msg_sent <- create_msg_sent("CommandFailed", list(message="failed to process comm data. lexical error: invalid char in json text.\n                                       just a string\n                     (right here) ------^\n"))
    t$handle_msg(msg_received)
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_fail_on_unsupported_command_type", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
        
    msg_received <- create_msg_received("UnsupportedCommand", list(name="x"))
    msg_sent <- create_msg_sent("CommandFailed", list(message="command \"UnsupportedCommand\" not supported"))
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received <- create_msg_received("SendValue", list(
                        formattedValue=list(mimeType="application/json", value="\"test\""),
                        name="x"
                    ))
    
    msg_sent <- create_msg_sent("CommandSucceeded")
    
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
    expect_equal(get("x"), "test", info="value was not set as expected")
    
    remove("x", envir=globalenv())
})

test_that("test_can_handle_send_value_with_dataframe", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())

    data <- fromJSON('[
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Black, 58"},
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Red, 58"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Red"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Black"}
        ]')
    
    df_expected = data.frame(data)
    
    msg_received <- create_msg_received("SendValue", list(
            formattedValue=list(
                mimeType="application/table-schema+json",
                value=wrap_as_json(list(
                    schema=list(
                        fields=fromJSON('[
                            {"name":"CategoryName","type":"string"},
                            {"name":"ProductName","type":"string"}
                        ]'),
                        primaryKey=list()
                    ),
                    data=data
                ))
            ),
            name="df_sent"
        )
    )
    
    msg_sent <- create_msg_sent("CommandSucceeded")
    
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
    expect_equal(get("df_sent"), df_expected, info="data.frame was not set as expected")
    
    remove("df_sent", envir=globalenv())
})

test_that("test_can_handle_unsupported_mimetype_in_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received <- create_msg_received("SendValue", list(
                        formattedValue=list(mimeType="application/unsupported", value="\"test\""),
                        name="x"
                    ))
    
    msg_sent <- create_msg_sent("CommandFailed", list(message="Failed to set value for \"x\". \"application/unsupported\" mimetype not supported."))
    
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_invalid_identifier_in_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received <- create_msg_received("SendValue", list(
                        formattedValue=list(mimeType="application/json", value="\"test\""),
                        name="_xy"
                    ))
    
    msg_sent <- create_msg_sent("CommandFailed", list(message="Invalid Identifier: \"_xy\""))
    
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_request_value_and_get_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    assign("test_var", "test_value", globalenv());
        
    msg_received <- create_msg_received("RequestValue", list(mimeType="application/json", name="test_var"))
    msg_sent <- create_msg_sent("ValueProduced", 
                                list(name="test_var", value="test_value", formattedValue=list(mimeType="application/json", value=wrap_as_json("test_value"))),
                                msg_received
                               )
    
    t$handle_msg(msg_received)
    remove("test_var", envir=globalenv())
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_request_dataframe_and_get_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())

    data <- fromJSON('[
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Black, 58"},
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Red, 58"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Red"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Black"}
        ]')
    
    assign("df_set", data.frame(data), globalenv());
    
    msg_received <- create_msg_received("RequestValue", list(mimeType="application/json", name="df_set"))
    msg_sent <- create_msg_sent("ValueProduced", list(
                    name="df_set",
                    value=data,
                    formattedValue=list(mimeType="application/table-schema+json", value=wrap_as_json(df_set))
                ), msg_received)
    
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
    remove("df_set", envir=globalenv())
})

test_that("test_can_handle_unknown_variable_request_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received <- create_msg_received("RequestValue", list(mimeType="application/json", name="unknown_var"))
    msg_sent <- create_msg_sent("CommandFailed", list(message="Variable \"unknown_var\" not found."))
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})

test_that("test_can_handle_request_value_infos_and_get_values", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    assign("test_var", "test_value", globalenv());
    assign("df_var", data.frame(fromJSON('[{"x": 123}, {"x": 456}]')), globalenv())
    
    msg_received <- create_msg_received("RequestValueInfos")
    msg_sent <- create_msg_sent("ValueInfosProduced", 
                                list(valueInfos=fromJSON('[{"name":"df_var", "formattedValue":{"mimeType":"application/table-schema+json", "value":"[{\\"x\\":123},{\\"x\\":456}]"}, "typeName":"data.frame"}, {"name":"test_var","formattedValue":{"mimeType":"application/json", "value":"\\"test_value\\""},"typeName":"character"}]')),
                                msg_received
                               )
    
    t$handle_msg(msg_received)
    remove("test_var", envir=globalenv())
    remove("df_var", envir=globalenv())
    expect_equal(t$msg_sent, msg_sent, info=sprintf('expected: %s\n  actual: %s', msg_sent, t$msg_sent))
})