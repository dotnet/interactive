using Microsoft.DotNet.Interactive.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;

namespace Microsoft.DotNet.Interactive.Messages
{
    public abstract class KernelChannelMessage
    {
        protected KernelChannelMessage(string label)
        {
            Label = label;
        }

        public string Label { get; }

        public abstract object PayloadForSerializationModel();

        public static KernelChannelMessage Deserialize(string json)
        {
            var jsonObject = JObject.Parse(json);

            return Deserialize(jsonObject);
        }

        internal static KernelChannelMessage Deserialize(JToken json)
        {
            string label = json["label"].Value<string>();
            KernelChannelMessage result = label switch
            {
                CommandKernelMessage.MessageLabel => new CommandKernelMessage(KernelCommandEnvelope.Deserialize(json["payload"]).Command),
                EventKernelMessage.MessageLabel => new EventKernelMessage(KernelEventEnvelope.Deserialize(json["payload"]).Event),
                _ => null
            };
            return result;
        }

        public static string Serialize(KernelChannelMessage channelMessage)
        {
            return JsonConvert.SerializeObject(
                SerializeToModel(channelMessage),
                Serializer.JsonSerializerSettings);
        }

        public static SerializationModel SerializeToModel(KernelChannelMessage channelMessage)
        {
            return new SerializationModel
            {
                Label = channelMessage.Label,
                Payload = channelMessage.PayloadForSerializationModel()
            };
        }

        public class SerializationModel
        {
            public string Label { get; set; }
            public object Payload { get; set; }
        }
    }
}
