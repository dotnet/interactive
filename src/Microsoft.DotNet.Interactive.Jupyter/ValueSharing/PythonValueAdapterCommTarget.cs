using System.Text;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class PythonValueAdapterCommTarget : IValueAdapterCommDefinition
    {
        private const string _commTargetDefinition = @"
class __ValueAdapterCommTarget: 
    _control_comm = None
    
    @classmethod
    def handle_control_comm_opened(cls, comm, msg):
        cls._control_comm = comm
        cls._control_comm.on_msg(cls._handle_control_comm_msg)
        
        cls._control_comm.send({'type': 'event', 'event': 'initialized'})
        
    @classmethod
    def _handle_control_comm_msg(cls, msg):
        # This shouldn't happen unless someone calls this method manually
        if cls._control_comm is None:
            raise RuntimeError('Control comm has not been properly opened')
            
        data = msg['content']['data']
        messageType = data['type']
        
        # cls.__debugLog('__commDebug.last_comm_data_recv', data)
        
        if (messageType == 'request'):
            cls._handle_adapter_request(data)
    
    @classmethod
    def _handle_adapter_request(cls, data):
        command = data['command']
        arguments = data['arguments']
        
        if (command == 'setVariable'): 
            cls._handle_setVariableRequest(command, arguments)
        elif (command == 'getVariable'):
            cls._handle_getVariableRequest(command, arguments)
    
    @classmethod
    def _handle_getVariableRequest(cls, command, variableInfo):
        var_name = variableInfo['name']
        var_type = variableInfo['type']
        
        if (var_name in globals()):
            rawValue = globals()[var_name]
            
            try: 
                import pandas as pd; 
                if (isinstance(rawValue, pd.DataFrame)):
                    var_type = 'application/table-schema+json'
                    rawValue = rawValue.to_dict('records')
            except Exception as e: 
                cls.__debugLog('__commDebug.dataframe.geterror', e)
                pass

            if (rawValue is not None): 
                cls._sendResponse(command, True, {
                    'name': var_name, 
                    'value': rawValue, 
                    'type': var_type
                })
            else: 
                cls._sendResponse(command, False)
        else: 
            cls._sendResponse(command, False)
            
    @classmethod
    def _handle_setVariableRequest(cls, command, variableInfo):
        var_name = variableInfo['name']
        var_type = variableInfo['type']
        var_value = variableInfo['value']
        
        resultValue = var_value
        if (var_type == 'application/table-schema+json'):
            try:
                import pandas as pd; resultValue = pd.DataFrame(data=var_value['Data'])
            except Exception as e:
                cls.__debugLog('__commDebug.dataframe.error', e)
                import json; resultValue = json.loads(var_value)
        elif (var_type == 'application/json'):
            import json; resultValue = json.loads(var_value)
        else:
            resultValue = var_value
            
        if (resultValue is not None): 
            cls._setVariable(var_name,resultValue) 
            cls._sendResponse(command, True, {
                'name': var_name, 
                'type': str(type(resultValue))
            })
        else: 
            cls._sendResponse(command, False)
            
    @classmethod
    def _setVariable(cls, name, value):
        globals()[name] = value
        
    @classmethod
    def _sendResponse(cls, command, success = False, body = None):
        response = {
            'type': 'response', 
            'command': command, 
            'success': success
        } 
        
        if (body is not None):
            response['body'] = body
        
        # print (response)
        cls._control_comm.send(response)

    @classmethod
    def __debugLog(cls, event, message):
        globals()[event] = message

";

        public string GetTargetDefinition(string targetName)
        {
            StringBuilder builder = new StringBuilder(_commTargetDefinition);
            builder.AppendLine($"get_ipython().kernel.comm_manager.register_target('{targetName}', __ValueAdapterCommTarget.handle_control_comm_opened)");

            return builder.ToString();
        }
    }
}
