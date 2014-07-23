using System;
using System.Collections.Generic;
using PollingTcp.Client;
using PollingTcp.Common;

namespace PollingTcp.Tests.Helper
{
    class ClientTestNetworkLinkLayer : TestNetworkLinkLayer, IClientNetworkLinkLayer
    {
        private readonly List<byte[]> receivedByteses = new List<byte[]>();
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public void StartPolling()
        {
        }

        public void StopPolling()
        {
        }

        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            EventHandler<DataReceivedEventArgs> handler = this.DataReceived;
            if (handler != null) handler(this, e);
        }

        public void Receive(byte[] data)
        {
            this.receivedByteses.Add(data);
            
            this.OnDataReceived(new DataReceivedEventArgs()
            {
                Bytes = data
            });
        }

        public override List<byte[]> ReceivedByteses
        {
            get { return new List<byte[]>(this.receivedByteses); }
        }
    }
}