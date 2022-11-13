import json
class __CommandEventCommTarget:
    _control_comm = None
    _coe_adapter = None
    _is_debug = False
    
    @classmethod
    def handle_control_comm_opened(cls, comm, msg):
        cls._control_comm = comm
        if (comm is not None):
            cls._control_comm.on_msg(cls.handle_control_comm_msg)
        
        cls._coe_adapter = cls.CommandEventAdapter()
        cls.__send_control_comm_msg(cls._coe_adapter.is_ready())
    
    @classmethod
    def handle_control_comm_msg(cls, msg):
        # This shouldn't happen unless someone calls this method manually
        if cls._control_comm is None and not cls._is_debug:
            raise RuntimeError('Control comm has not been properly opened')
        
        data = msg['content']['data']
        response = cls._coe_adapter.handle_command(data)
        cls.__send_control_comm_msg(response)
        
    @classmethod
    def __send_control_comm_msg(cls, payload):
        if cls._is_debug:
            print (payload)
        else:
            cls._control_comm.send(payload)
    
    
    
    class CommandEventAdapter:          
                
        def handle_command(self, data):
            try: 
                commandOrEvent = json.loads(data['commandOrEvent'])
                self.__debugLog('handle_command.last_data_recv', commandOrEvent)
                
                commandType = commandOrEvent['commandType']
                command = commandOrEvent['command']
                
                if (commandType == 'SendValue'):
                    command = self.SendValue(command)
                    return self.__handle_send_value(command)
                elif (commandType == 'RequestValue'):
                    command = self.RequestValue(command)
                    return self.__handle_request_value(command)
                else: 
                    return self.__envelop(self.CommandFailed(f'command "{commandType}" not supported'))
                
                
            except Exception as e: 
                self. __debugLog('handle_command.commandFailed', e)
                return self.__envelop(self.CommandFailed('failed to process comm data'))

        def __handle_request_value(self, requestValue):
            name = requestValue.name
            mimeType = requestValue.mimeType
            
            if (name not in globals()):
                return self.__envelop(self.CommandFailed(f'"{name}" not found.'))
            
            rawValue = globals()[name]
            
            try: 
                import pandas as pd; 
                if (isinstance(rawValue, pd.DataFrame)):
                    mimeType = 'application/table-schema+json'
                    rawValue = rawValue.to_dict('records')
            except Exception as e: 
                self. __debugLog('__handle_request_value.dataframe.error', e)
                pass

            formattedValue = self.FormattedValue(mimeType) # This will be formatted in the .NET kernel
            
            if (rawValue is not None): 
                return self.__envelop(self.ValueProduced(name, rawValue, formattedValue))
            
            return self.__envelop(self.CommandFailed(f'Failed to get value for "{name}"'))
        
        def __handle_send_value(self, sendValue):
            mimeType = sendValue.formattedValue['mimeType']
            name = sendValue.name
            resultValue = sendValue.formattedValue['value']
            
            if (not str.isidentifier(name)):
                return self.__envelop(self.CommandFailed(f'Invalid Identifier: "{name}"'))
        
            if (mimeType == 'application/json'):
                import json; resultValue = json.loads(resultValue)
            elif (mimeType == 'application/table-schema+json'):
                import json; resultValue = json.loads(resultValue)
                try:
                    import pandas as pd; resultValue = pd.DataFrame(data=resultValue['data'])
                except Exception as e:
                    self. __debugLog('__handle_send_value.dataframe.error', e)
                    return self.__envelop(self.CommandFailed(f'Pandas not installed. Cannot create dataframe for: "{name}"'))
                
            if (resultValue is not None): 
                self.__setVariable(name, resultValue) 
                return self.__envelop(self.CommandSucceeded())
            
            return self.__envelop(self.CommandFailed(f'Failed to set value for "{name}"'))
        
        def is_ready(self):
            return self.__envelop(self.KernelReady())
    
        def __envelop(self, event):
            return self.EventEnvelope(event).payload()
        
        @classmethod
        def __setVariable(cls, name, value):
            globals()[name] = value
        
        @staticmethod
        def __debugLog(event, message):
            globals()[f'__log_coe_adapter.{str(event)}'] = message
    
    
        class KernelCommand: 
            pass

        class SendValue(KernelCommand): 
            def __init__(self, entries):
                self.__dict__.update(**entries)
        
        class RequestValue(KernelCommand):
            def __init__(self, entries):
                self.__dict__.update(**entries)
                
        class FormattedValue:
            def __init__(self, mimeType, value = None):
                self.mimeType = mimeType
                self.value = value
                
        class KernelEvent:
            pass

        class KernelReady(KernelEvent):
            pass

        class CommandSucceeded(KernelEvent):
            pass

        class CommandFailed(KernelEvent):
            def __init__(self, message = None):
                self.message = message
        
        class ValueProduced(KernelEvent):
            def __init__(self, name, value, formattedValue = None):
                self.name = name
                self.value = value 
                self.formattedValue = formattedValue
                
        class Envelope:
            def payload(self):
                return { 'commandOrEvent': self.__to_json_string(self) }
            
            @staticmethod
            def __to_json_string(obj):
                return json.dumps(obj, default=lambda o: o.__dict__)
        
        class EventEnvelope(Envelope):
            def __init__(self, event = None):
                self.event = event
                self.eventType = type(event).__name__
            
            def payload(self):
                ret = super().payload()
                ret['type'] = 'event'
                return ret
    
        
get_ipython().kernel.comm_manager.register_target('dotnet_coe_adapter_comm', __CommandEventCommTarget.handle_control_comm_opened)