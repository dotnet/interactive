﻿namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    public static class ValueAdapterMessageType
    {
        public const string Request = "request";
        public const string Response = "response";
        public const string Event = "event";
    }

    public static class ValueAdapterCommandTypes
    {
        public const string SetVariable = "setVariable";
        public const string GetVariable = "getVariable";
        public const string Variables = "variables";
    }

    public static class ValueAdapterEventTypes
    {
        public const string Initialized = "initialized";
    }
}