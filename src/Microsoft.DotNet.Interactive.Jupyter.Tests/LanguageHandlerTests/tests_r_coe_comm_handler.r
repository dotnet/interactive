# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

source('../../Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers/r/coe_comm_handler.r')

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
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent), )
})

test_that("test_can_handle_send_value", {
    t <- testComm()
    .dotnet_coe_comm_hander_env$coe_handler_connect_to_comm(t, list())
    
    msg_received = fromJSON('{"content": {"data": {"type": "command", "commandOrEvent": "{\\"token\\":\\"19\\",\\"id\\":\\"ccc7591568d943c9bbe7dd8254e89b0d\\",\\"commandType\\":\\"SendValue\\",\\"command\\":{\\"formattedValue\\":{\\"mimeType\\":\\"application/json\\",\\"value\\":\\"\\\\\\"test\\\\\\"\\"},\\"name\\":\\"x\\",\\"targetKernelName\\":\\"python\\",\\"originUri\\":null,\\"destinationUri\\":null},\\"routingSlip\\":[\\"kernel://pid-17796/python\\"]}"}}}')
    msg_sent <- fromJSON('{"commandOrEvent":"{\\"event\\":{},\\"eventType\\":\\"CommandSucceeded\\",\\"command\\":null}","type":"event"}') 
    t$handle_msg(msg_received)
    expect_equal(t$msg_sent, t$wrap_as_json(msg_sent))
})
