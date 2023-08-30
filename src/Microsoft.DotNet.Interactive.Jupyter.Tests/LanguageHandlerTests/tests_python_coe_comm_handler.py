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

    def __init__(self, methodName: str = ...) -> None:
        super().__init__(methodName)
        self.maxDiff = None

    @staticmethod
    def create_msg_received(commandType, commandReceived = None):
        command = {
             "targetKernelName":"python",
             "originUri":None,
             "destinationUri":None
        }
        if commandReceived is not None:
            command |= commandReceived
        return {
            "content": {
                "data": {
                    'type': 'command',
                    'commandOrEvent': json.dumps(
                        {
                            "token": "19",
                            "id": "ccc7591568d943c9bbe7dd8254e89b0d",
                            "commandType": commandType,
                            "command": command,
                            "routingSlip": ["kernel://pid-17796/python"],
                        }
                    ),
                }
            }
        }
    
    @staticmethod
    def create_msg_sent(eventType, eventSent = {}, commandJsonString = None):
        command = None if commandJsonString is None else json.loads(commandJsonString)
        return {
            "commandOrEvent": json.dumps(
                {"event": eventSent, "eventType": eventType, "command": command}
            ),
            "type": "event",
        }
        
    def setUp(self):
        self.comm = testComm()
        self.handler = get_dotnet_coe_comm_handler()
        self.handler.handle_control_comm_opened(self.comm, 'test_target')
        
    def test_can_get_kernel_ready_on_comm_open(self):
        self.assertEqual(self.comm.msg_sent, self.create_msg_sent("KernelReady", { "kernelInfos": [] }))
    
    def test_can_handle_invalid_json(self):
        msg_received = {"content": { "data": {'type': 'command', 'commandOrEvent': 'just a string'}}};
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "failed to process comm data. Expecting value: line 1 column 1 (char 0)"
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_fail_on_unsupported_command_type(self):
        msg_received = self.create_msg_received("UnsupportedCommand", {"name":"x"});
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "command \"UnsupportedCommand\" not supported"
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_send_value(self):
        msg_received = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/json",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandSucceeded")
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers. With .net interactive they will be.
        self.assertEqual(coe_comm_handler.x, "test", "variable is not set")
    
    def test_can_handle_send_value_with_dataframe(self):
        data = [
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Black, 58"},
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Red, 58"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Red"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Black"}
        ];
        
        import pandas as pd
        df_expected = pd.DataFrame(data)
        from pandas.testing import assert_frame_equal
        
        msg_received = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/table-schema+json",
                "value": json.dumps({
                    "schema": {
                        "fields":[
                            {"name":"CategoryName","type":"string"},
                            {"name":"ProductName","type":"string"}
                        ],
                        "primaryKey":[]
                    },
                    "data": data
                })
            },
            "name":"df_sent"
        });
        
        msg_sent = self.create_msg_sent("CommandSucceeded")
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        assert_frame_equal(coe_comm_handler.df_sent, df_expected)
                
    def test_can_handle_unsupported_mimetype_in_send_value(self):
        msg_received = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/unsupported",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Failed to set value for \"x\". \"application/unsupported\" mimetype not supported."
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_invalid_value_for_dataframe_in_send_value(self):
        msg_received = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/table-schema+json",
                "value":"\"test\""
            },
            "name":"x"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Cannot create pandas dataframe for: \"x\". string indices must be integers"
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_invalid_identifier_in_send_value(self):
        msg_received = self.create_msg_received("SendValue", {
            "formattedValue":{
                "mimeType":"application/json",
                "value":"\"test\""
            },
            "name":"x.y"
        });
        
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Invalid Identifier: \"x.y\""
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_request_value_and_get_value(self):
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers. With .net interactive they will be.
        coe_comm_handler.x = "test"
        msg_received = self.create_msg_received("RequestValue", {"name": "x", "mimeType": "application/json"});
        msg_sent = self.create_msg_sent("ValueProduced", {
            "name":"x",
            "value":"test",
            "formattedValue":{
                "mimeType":"application/json",
                "value": json.dumps("test")
            }
        }, msg_received["content"]["data"]["commandOrEvent"])
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
    def test_can_handle_request_dataframe_and_get_value(self):
        data = [
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Black, 58"},
            {"CategoryName":"Road Frames","ProductName":"HL Road Frame - Red, 58"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Red"},
            {"CategoryName":"Helmets","ProductName":"Sport-100 Helmet, Black"}
        ];
        
        import pandas as pd
        coe_comm_handler.df_set = pd.DataFrame(data)
        msg_received = self.create_msg_received("RequestValue", {"name": "df_set", "mimeType": "application/json"});
        msg_sent = self.create_msg_sent("ValueProduced", {
            "name":"df_set",
            "value":data,
            "formattedValue":{
                "mimeType":"application/table-schema+json",
                "value": coe_comm_handler.df_set.to_string(index=False, max_rows=5)
            }
        }, msg_received["content"]["data"]["commandOrEvent"])
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
    
    def test_can_handle_unknown_variable_request_value(self):
        msg_received = self.create_msg_received("RequestValue", {"name": "unknown_var", "mimeType": "application/json"});
        msg_sent = self.create_msg_sent("CommandFailed", {
            "message": "Variable \"unknown_var\" not found."
        })
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
    def test_can_handle_request_value_infos_and_get_values(self):
        # below is just a test workaround as tests are not sharing the same 
        # namespace as the handlers, so for now we inject the same variable in both.
        # With .net interactive they will be.
        global x 
        x = 456
        coe_comm_handler.x = x
        
        import pandas as pd
        global df
        df = pd.DataFrame([{"x": 123}, {"x": 456}])
        coe_comm_handler.df = df
        
        msg_received = self.create_msg_received("RequestValueInfos");
        msg_sent = self.create_msg_sent("ValueInfosProduced", {
            "valueInfos": [
                {
                    "name": "df", 
                    "formattedValue": {
                        "mimeType":"application/table-schema+json",
                        "value": df.to_string(index=False, max_rows=5)
                    }, 
                    "typeName": "<class \'pandas.core.frame.DataFrame\'>"
                },
                {
                    "name": "x", 
                    "formattedValue": {
                        "mimeType":"application/json",
                        "value": "456"
                    }, 
                    "typeName": "<class \'int\'>"
                }]
        }, msg_received["content"]["data"]["commandOrEvent"])
        self.comm.handle_msg(msg_received)
        self.assertEqual(self.comm.msg_sent, msg_sent)
        
unittest.main(argv=[''], verbosity=2, exit=False)
