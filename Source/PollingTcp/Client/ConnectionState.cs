﻿namespace PollingTcp.Client
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Timeout, 
        Error
    }
}