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
    def create_msg_received(commandType, commandReceived):
        command = {
             "targetKernelName":"python",
             "originUri":None,
             "destinationUri":None
        }
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
        
    def test_kernel_ready(self):
        self.assertEqual(self.comm.msg_sent, self.create_msg_sent("KernelReady"))
        
    def test_handle_send_value(self):
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
     
    def test_handle_request_value(self):
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

unittest.main(argv=[''], verbosity=2, exit=False)
