# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Run this test using:
#       > ipython tests_python_coe_comm_handler.py

import sys
sys.path.append('../../Microsoft.DotNet.Interactive.Jupyter/CommandEvents/LanguageHandlers/python')

import json
import unittest
from coe_comm_handler import __get_dotnet_coe_comm_handler as get_dotnet_coe_comm_handler
import coe_comm_handler

class testComm:
    __msg_handler = None
    # message sent back to the comm channel
    msg_sent = None
    def handle_msg(self, msg):
        self.__msg_handler(msg)
        
    def on_msg(self, handler):
        self.__msg_handler = handler
    
    def send(self, msg):
        self.msg_sent = msg

class TestCoeCommHandler(unittest.TestCase):
    @staticmethod
    def create_msg_received(commandType, commandReceived = None):
        command = {
             "targetKernelName":"python",
             "originUri":None,
             "destinationUri":None
        }
        if commandReceived is not None:
            command.update(commandReceived)
        msg = {
             "content": { 
                 "data": {
                     'type': 'command', 
                     'commandOrEvent': json.dumps({
                         "token":"19",
                         "id":"ccc7591568d943c9bbe7dd8254e89b0d",
                         "commandType":commandType,
                         "command": command,
                         "routingSlip":["kernel://pid-17796/python"]
                     })
                 }
             }
        }
        
        return msg
    
    @staticmethod
    def create_msg_sent(eventType, eventSent = {}, commandJsonString = None):
        command = None if commandJsonString is None else json.loads(commandJsonString)
        msg = {
            "commandOrEvent": json.dumps({
                 "event": eventSent,
                 "eventType": eventType,
                 "command": command
            }), 
            "type": "event"
        }
        
        return msg
        
    def setUp(self):
        self.comm = testComm()
        self.handler = get_dotnet_coe_comm_handler()
        self.handler.handle_control_comm_opened(self.comm, 'test_target')
        
    def test_can_get_kernel_ready_on_comm_open(self):
        self.assertEqual(self.comm.msg_sent, self.create_msg_sent("KernelReady"))
        
    def test_can_handle_send_value(self):
        msg_recieved = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/json",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandSucceeded")
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers. With .net interactive they will be.
        self.assertEqual(coe_comm_handler.x, "test", "variable is not set")
        
    def test_can_handle_unsupported_mimetype_in_send_value(self):
        msg_recieved = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/unsupported",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Failed to set value for \"x\". \"application/unsupported\" mimetype not supported."
        })
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_invalid_value_for_dataframe_in_send_value(self):
        msg_recieved = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/table-schema+json",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Cannot create pandas dataframe for: \"x\". string indices must be integers"
        })
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_invalid_identifier_in_send_value(self):
        msg_recieved = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/json",
                "value":"\"test\""
            },
            "name":"x.y"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Invalid Identifier: \"x.y\""
        })
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
    def test_can_handle_request_value_and_get_value(self):
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers. With .net interactive they will be.
        coe_comm_handler.x = "test"
        msg_recieved = self.create_msg_received("RequestValue", {"name": "x", "mimeType": "application/json"});
        msg_sent = self.create_msg_sent("ValueProduced", {
            "name":"x",
            "value":"test",
            "formattedValue":{
                "mimeType":"application/json",
                "value":None
            }
        }, msg_recieved["content"]["data"]["commandOrEvent"])
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
    def test_can_handle_unknown_variable_request_value(self):
        msg_recieved = self.create_msg_received("RequestValue", {"name": "unknown_var", "mimeType": "application/json"});
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Variable \"unknown_var\" not found."
        })
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
    def test_can_fail_on_unsupported_command_type(self):
        msg_recieved = self.create_msg_received("UnsupportedCommand", {"name":"x"});
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "command \"UnsupportedCommand\" not supported"
        })
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_request_value_infos_and_get_values(self):
    
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers, so for now we inject the same variable in both.
        # With .net interactive they will be.
        global x 
        x = 456
        coe_comm_handler.x = x
        
        msg_recieved = self.create_msg_received("RequestValueInfos");
        msg_sent = self.create_msg_sent("ValueInfosProduced", {
            "valueInfos": [{"name": "x", "nativeType": "<class \'int\'>"}]
        }, msg_recieved["content"]["data"]["commandOrEvent"])
        self.comm.handle_msg(msg_recieved)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
unittest.main(argv=[''], verbosity=2, exit=False)
