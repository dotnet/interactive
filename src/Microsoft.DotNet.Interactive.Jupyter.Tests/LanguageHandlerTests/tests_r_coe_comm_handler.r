# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Run this test using:
#   copy and run both the coe_comm_handler.r and test script below to an R jupyter notebook 
#   TODO: Create a command line script to run as R script

# source('../../Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers/r/coe_comm_handler.r')

if (!require(testthat)) install.packages('testthat')
library(testthat)

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
        }, 
        wrap_as_json = function(msg) {
            return (toJSON(msg, auto_unbox = TRUE, null="null"))
        }
    )
)

test_that("test_can_get_kernel_ready_on_comm_open", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{},\\"eventType\\":\\"KernelReady\\",\\"command\\":null}","type":"event"}') 
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_invalid_json", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "just a string"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"message\\":\\"failed to process comm data. lexical error: invalid char in json text.\\\\n                                       just a string\\\\n                     (right here) ------^\\\\n\\"},\\"eventType\\":\\"CommandFailed\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_fail_on_unsupported_command_type", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "{\\"token\\":\\"19\\",\\"id\\":\\"ccc7591568d943c9bbe7dd8254e89b0d\\",\\"commandType\\":\\"UnsupportedCommand\\",\\"command\\":{\\"formattedValue\\":{\\"mimeType\\":\\"application/json\\",\\"value\\":\\"\\\\\\"test\\\\\\"\\"},\\"name\\":\\"x\\",\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"message\\":\\"command \\\\\\"UnsupportedCommand\\\\\\" not supported\\"},\\"eventType\\":\\"CommandFailed\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "{\\"token\\":\\"19\\",\\"id\\":\\"ccc7591568d943c9bbe7dd8254e89b0d\\",\\"commandType\\":\\"SendValue\\",\\"command\\":{\\"formattedValue\\":{\\"mimeType\\":\\"application/json\\",\\"value\\":\\"\\\\\\"test\\\\\\"\\"},\\"name\\":\\"x\\",\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{},\\"eventType\\":\\"CommandSucceeded\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
    expect_equal(get("x"), "test", info="value was not set as expected")
    remove("x", envir=globalenv())
})

test_that("test_can_handle_unsupported_mimetype_in_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "{\\"token\\":\\"19\\",\\"id\\":\\"ccc7591568d943c9bbe7dd8254e89b0d\\",\\"commandType\\":\\"SendValue\\",\\"command\\":{\\"formattedValue\\":{\\"mimeType\\":\\"application/unsupported\\",\\"value\\":\\"\\\\\\"test\\\\\\"\\"},\\"name\\":\\"x\\",\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"message\\":\\"Failed to set value for \\\\\\"x\\\\\\". \\\\\\"application/unsupported\\\\\\" mimetype not supported.\\"},\\"eventType\\":\\"CommandFailed\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_invalid_identifier_in_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "{\\"token\\":\\"19\\",\\"id\\":\\"ccc7591568d943c9bbe7dd8254e89b0d\\",\\"commandType\\":\\"SendValue\\",\\"command\\":{\\"formattedValue\\":{\\"mimeType\\":\\"application/json\\",\\"value\\":\\"\\\\\\"test\\\\\\"\\"},\\"name\\":\\"_xy\\",\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"message\\":\\"Invalid Identifier: \\\\\\"_xy\\\\\\"\\"},\\"eventType\\":\\"CommandFailed\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_request_value_and_get_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    assign("test_var", "test_value", globalenv());
    
    msg_received = fromJSON('{"content": {"data": {"type": "command","commandOrEvent": "{\\"token\\": \\"27\\", \\"id\\": \\"278e85b048f3499a9ae6fd4ecf0d80df\\", \\"commandType\\": \\"RequestValue\\", \\"command\\": {\\"name\\": \\"test_var\\", \\"mimeType\\": \\"application/json\\", \\"targetKernelName\\": \\"r\\", \\"originUri\\": null, \\"destinationUri\\": null}, \\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"name\\":\\"test_var\\",\\"value\\":\\"test_value\\",\\"formattedValue\\":{\\"mimeType\\":\\"application/json\\"}},\\"eventType\\":\\"ValueProduced\\",\\"command\\":{\\"token\\":\\"27\\",\\"id\\":\\"278e85b048f3499a9ae6fd4ecf0d80df\\",\\"commandType\\":\\"RequestValue\\",\\"command\\":{\\"name\\":\\"test_var\\",\\"mimeType\\":\\"application/json\\",\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[]}}","type":"event"}') 
    t$handle_msg(msg_received)
    remove("test_var", envir=globalenv())
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_unknown_variable_request_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command","commandOrEvent": "{\\"token\\": \\"27\\", \\"id\\": \\"278e85b048f3499a9ae6fd4ecf0d80df\\", \\"commandType\\": \\"RequestValue\\", \\"command\\": {\\"name\\": \\"unknown_var\\", \\"mimeType\\": \\"application/json\\", \\"targetKernelName\\": \\"r\\", \\"originUri\\": null, \\"destinationUri\\": null}, \\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"message\\":\\"Variable \\\\\\"unknown_var\\\\\\" not found.\\"},\\"eventType\\":\\"CommandFailed\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})

test_that("test_can_handle_request_value_infos_and_get_values", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    assign("test_var", "test_value", globalenv());
    
    msg_received = fromJSON('{"content": {"data": {"type": "command","commandOrEvent": "{\\"token\\": \\"27\\", \\"id\\": \\"278e85b048f3499a9ae6fd4ecf0d80df\\", \\"commandType\\": \\"RequestValueInfos\\", \\"command\\": {\\"targetKernelName\\": \\"r\\", \\"originUri\\": null, \\"destinationUri\\": null}, \\"routingSlip\\":[\\"kernel://pid-17796/r\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{\\"valueInfos\\":[{\\"name\\":\\"test_var\\",\\"nativeType\\":\\"character\\"}]},\\"eventType\\":\\"ValueInfosProduced\\",\\"command\\":{\\"token\\":\\"27\\",\\"id\\":\\"278e85b048f3499a9ae6fd4ecf0d80df\\",\\"commandType\\":\\"RequestValueInfos\\",\\"command\\":{\\"targetKernelName\\":\\"r\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[]}}","type":"event"}') 
    t$handle_msg(msg_received)
    remove("test_var", envir=globalenv())
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), info=sprintf('expected: %s\n  actual: %s', t$wrap_as_json(msg_sent), t$msg_sent))
})
