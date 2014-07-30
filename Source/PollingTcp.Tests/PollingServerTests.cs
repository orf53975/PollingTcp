﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Frame;
using PollingTcp.Server;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingServerTests
    {
        private ClientDataFrame initConnectionFrame = new ClientDataFrame() { SequenceId = 7 };

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InitializeServer_WithtNoLinkLayer_ShouldRaiseException()
        {
            var server = new TestPollingServer(null, 10, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void NewServer_TryToAcceptSessionButDidNotStartYet_ShouldThrowAnException()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer, 10, 10);

            server.Accept();
        }

        [TestMethod]
        public void NewServer_ReceivesConnectionRequest_NothingHappens()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer, 10, 10);

            networkLayer.Receive(new BinaryClientFrameEncoder().Encode(this.initConnectionFrame));
            Assert.AreEqual(0, server.SessionCount);
        }

        [TestMethod]
        public void StartedServer_ReceivesEmptyClientId_ShouldUnblockAcceptAndReturnSession()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer, 10, 10);

            ClientSession<ClientDataFrame, ServerDataFrame> session = null;

            server.Start();

            session = this.WaitForClientSession(server, networkLayer);

            Assert.IsNotNull(session, "There was no session captured");
        }

        private ClientSession<ClientDataFrame, ServerDataFrame> WaitForClientSession(TestPollingServer server, ServerTestNetworkLinkLayer networkLayer)
        {
            ClientSession<ClientDataFrame, ServerDataFrame> session = null;
            var resetEvent = new AutoResetEvent(false);
            var t = new Task(() =>
            {
                session = server.Accept();
                resetEvent.Set();
            });

            t.Start();

            while (!resetEvent.WaitOne(10))
            {
                networkLayer.Receive(new BinaryClientFrameEncoder().Encode(this.initConnectionFrame));
            }
            return session;
        }

        [TestMethod]
        public void StartedServer_ReceivesEmptyClientId_ShouldResponseWithAClientId()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer, 10, 10);

            server.Start();

            this.WaitForClientSession(server, networkLayer);

            networkLayer.Receive(new BinaryClientFrameEncoder().Encode(initConnectionFrame));

            Assert.IsTrue(networkLayer.SentBytes.Any(), "There should be at least one captured frame");
            
            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ServerDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.IsNotNull(sentFrame.Payload);
            Assert.AreNotEqual(0, BitConverter.ToInt32(sentFrame.Payload, 0));
        }

        [TestMethod]
        public void EstablishedSession_ReceivesData_ShouldTriggerEventOnSession()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer, 10, 10);

            var serverOnFrameReceived = new List<ClientDataFrame>();

            server.Start();
            
            var session = this.WaitForClientSession(server, networkLayer);
            session.FrameReceived += (sender, args) => serverOnFrameReceived.Add(args.Frame);

            // Find out the ClientId
            Assert.IsTrue(networkLayer.SentBytes.Any(), "There should be at least one captured frame");

            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ServerDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.IsNotNull(sentFrame.Payload);
            Assert.AreNotEqual(0, BitConverter.ToInt32(sentFrame.Payload, 0));

            var clientId = BitConverter.ToInt32(sentFrame.Payload, 0);
            var clientFrame = new ClientDataFrame()
            {
                ClientId = clientId,
                SequenceId = initConnectionFrame.SequenceId + 1,
                Payload = Encoding.UTF8.GetBytes("Hello World")
            };

            networkLayer.Receive(new GenericSerializer<ClientDataFrame>().Serialize(clientFrame));

            Assert.AreEqual(1, serverOnFrameReceived.Count);
            Assert.AreEqual("Hello World", Encoding.UTF8.GetString(serverOnFrameReceived[0].Payload));
        }
    }

    class TestPollingServer : PollingServer<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingServer(IServerNetworkLinkLayer networkLinkLayer, int maxIncomingSequenceValue, int maxOutgoingSequenceValue) : 
            base(networkLinkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), maxIncomingSequenceValue, maxOutgoingSequenceValue)
        {
        }
    }
}
