# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

try:
    get_ipython().__class__.__name__
except NameError:
    raise Exception("This script needs to be run in ipython")
    
import json
def __get_dotnet_coe_comm_handler(): 


    class CommandEventCommTarget:
        __control_comm = None
        __coe_handler = None

        def handle_control_comm_opened(self, comm, msg):
            if comm is None:
                raise RuntimeError('Control comm required to open')

            self.__control_comm = comm
            self.__control_comm.on_msg(self.handle_control_comm_msg)

            self.__coe_handler = CommandEventHandler()
            self.__control_comm.send(self.__coe_handler.is_ready())

        def handle_control_comm_msg(self, msg):
            # This shouldn't happen unless someone calls this method manually
            if self.__control_comm is None and not self._is_debug:
                raise RuntimeError('Control comm has not been properly opened')

            data = msg['content']['data']
            response = self.__coe_handler.handle_command_or_event(data)
            self.__control_comm.send(response)




    class CommandEventHandler:
        __exclude_types = ["<class 'module'>"]


        def handle_command_or_event(self, data):
            try:
                msg_type = data['type']
                commandOrEvent = json.loads(data['commandOrEvent'])
                # self.__debugLog('handle_command_or_event.last_data_recv', commandOrEvent)

                if (msg_type == "command"):
                    return self.__handle_command(commandOrEvent)

            except Exception as e: 
                self. __debugLog('handle_command_or_event.commandFailed', e)
                return EventEnvelope(CommandFailed(f'failed to process comm data. {str(e)}')).payload()

        def __handle_command(self, commandOrEvent):
            commandType = commandOrEvent['commandType']

            envelop = None
            if (commandType == SendValue.__name__):
                envelop = self.__handle_send_value(commandOrEvent)
            elif (commandType == RequestValue.__name__):
                envelop = self.__handle_request_value(commandOrEvent)
            elif (commandType == RequestValueInfos.__name__):
                envelop = self.__handle_request_value_infos(commandOrEvent)
            else: 
                envelop = EventEnvelope(CommandFailed(f'command "{commandType}" not supported'))

            return envelop.payload()

        def __handle_request_value_infos(self, command):
            results_who_ls = get_ipython().run_line_magic('who_ls', '')
            variables = globals()
            results = [KernelValueInfo(x, FormattedValue.fromValue(variables[x]), str(type(variables[x]))) 
                                    for x in results_who_ls 
                                    if x in variables and str(type(variables[x])) not in self.__exclude_types]


            return EventEnvelope(ValueInfosProduced(results), command)

        def __handle_request_value(self, command):
            requestValue = RequestValue(command['command'])
            name = requestValue.name
            mimeType = requestValue.mimeType

            if (name not in globals()):
                return EventEnvelope(CommandFailed(f'Variable "{name}" not found.'))

            rawValue = globals()[name]
            updatedValue = None

            try: 
                import pandas as pd; 
                if (isinstance(rawValue, pd.DataFrame)):
                    mimeType = 'application/table-schema+json'
                    updatedValue = rawValue.to_dict('records')
            except Exception as e: 
                self. __debugLog('__handle_request_value.dataframe.error', e)
            formattedValue = FormattedValue.fromValue(rawValue, mimeType) 

            return EventEnvelope(ValueProduced(name, rawValue if updatedValue is None else updatedValue, formattedValue), command)

        def __handle_send_value(self, command):
            sendValue = SendValue(command['command'])
            mimeType = sendValue.formattedValue['mimeType']
            name = sendValue.name
            rawValue = sendValue.formattedValue['value']
            resultValue = None

            if (not str.isidentifier(name)):
                return EventEnvelope(CommandFailed(f'Invalid Identifier: "{name}"'))

            if (mimeType == 'application/json'):
                import json; resultValue = json.loads(rawValue)
            elif (mimeType == 'application/table-schema+json'):
                import json; resultValue = json.loads(rawValue)
                try:
                    import pandas as pd; resultValue = pd.DataFrame(data=resultValue['data'])
                except Exception as e:
                    self.__debugLog('__handle_send_value.dataframe.error', e)
                    return EventEnvelope(CommandFailed(f'Cannot create pandas dataframe for: "{name}". {str(e)}'))

            if (resultValue is not None): 
                self.__setVariable(name, resultValue) 
                return EventEnvelope(CommandSucceeded())

            return EventEnvelope(CommandFailed(f'Failed to set value for "{name}". "{mimeType}" mimetype not supported.'))

        def is_ready(self):
            return EventEnvelope(KernelReady()).payload()

        @staticmethod
        def __setVariable(name, value):
            globals()[name] = value

        @staticmethod
        def __debugLog(event, message):
            globals()[f'__log__coe_handler.{str(event)}'] = message



    class KernelCommand: 
        pass

    class SendValue(KernelCommand): 
        def __init__(self, entries):
            self.__dict__.update(**entries)

    class RequestValue(KernelCommand):
        def __init__(self, entries):
            self.__dict__.update(**entries)

    class RequestValueInfos(KernelCommand):
        def __init__(self, entries):
            self.__dict__.update(**entries)

    class FormattedValue:
        def __init__(self, mimeType = 'application/json', value = None):
            self.mimeType = mimeType
            self.value = value

        @staticmethod
        def fromValue(value, mimeType = 'application/json'):
            formattedValue = None
            try: 
                import pandas as pd; 
                if (isinstance(value, pd.DataFrame)):
                    mimeType = 'application/table-schema+json'
            except Exception: 
                pass

            if (mimeType == 'application/json'):
                import json; formattedValue = json.dumps(value)
            elif (mimeType == 'application/table-schema+json'):
                formattedValue = value.to_string(index=False, max_rows=5)

            return FormattedValue(mimeType, formattedValue)

    class KernelValueInfo:
        def __init__(self, name, formattedValue: FormattedValue, typeName = None):
            self.name = name
            self.formattedValue = formattedValue
            self.typeName = typeName

    class KernelEvent:
        pass

    class KernelReady(KernelEvent):
        def __init__(self, kernelInfos = []):
            self.kernelInfos = kernelInfos

    class CommandSucceeded(KernelEvent):
        pass

    class CommandFailed(KernelEvent):
        def __init__(self, message = None):
            self.message = message

    class ValueProduced(KernelEvent):
        def __init__(self, name, value, formattedValue: FormattedValue):
            self.name = name
            self.value = value 
            self.formattedValue = formattedValue

    class ValueInfosProduced(KernelEvent):
        def __init__(self, valueInfos):
            self.valueInfos = valueInfos

    class Envelope:
        def payload(self):
            return { 'commandOrEvent': self.__to_json_string(self) }

        @staticmethod
        def __to_json_string(obj):
            return json.dumps(obj, default=lambda o: o.__dict__)

    class EventEnvelope(Envelope):
        def __init__(self, event: KernelEvent = None, command = None):
            self.event = event
            self.eventType = type(event).__name__
            self.command = command

        def payload(self):
            ret = super().payload()
            ret['type'] = 'event'
            return ret

    return CommandEventCommTarget()

if hasattr(get_ipython(), 'kernel'):
    get_ipython().kernel.comm_manager.register_target('dotnet_coe_handler_comm', __get_dotnet_coe_comm_handler().handle_control_comm_opened)
