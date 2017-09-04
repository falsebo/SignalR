// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class TestConnection : PersistentConnection
    {
        internal static ConnectionBehavior Behavior { get; set; }
        private static ConcurrentQueue<string> _sendTask = new ConcurrentQueue<string>();
        private long t = 0;
        private static object _syncObject = new object();
        private static bool start = false;
        public TestConnection()
        {
            t = DateTime.Now.Ticks;
            if (start)
            {
                return;
            }
            start = true;
            Task.Run(() =>
            {
                SaveReceiptTask();
            });
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            //if (Behavior == ConnectionBehavior.Echo)
            //{
            //    Connection.Send(connectionId, data);
            //}
            //else if (Behavior == ConnectionBehavior.Broadcast)
            //{
            //    Connection.Broadcast(data);
            //}

            var model = JsonConvert.DeserializeObject<TestMessageData>(data);
            if (model != null)
            {
                var now = DateTime.Now;
                model.SenderId = connectionId;
                model.ServerTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                model.ServerTicks = now.Ticks;
                model.MessageId = string.IsNullOrWhiteSpace(model.MessageId) ? Guid.NewGuid().ToString() : model.MessageId;
                switch (model.MessageType)
                {
                    case TestMessageType.Group:
                        base.Groups.Send(model.Target, model, connectionId);
                        break;
                    case TestMessageType.Single:
                        Connection.Send(string.IsNullOrWhiteSpace(model.Target) ? model.Target : model.Target, JsonConvert.SerializeObject(model));
                        break;
                    case TestMessageType.Receipt://回执
                        //接收到客户端回执情况记录到内存中
                        _sendTask.Enqueue(JsonConvert.SerializeObject(model));
                        break;
                }
            }
            return Task.FromResult<object>(null);
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            var groupId = request.QueryString.Get("group");
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                base.Groups.Add(connectionId, groupId);
            }
            return base.OnConnected(request, connectionId);
        }

        private string MessageData(string data, string connectionId, string target)
        {
            var now = DateTime.Now;
            return string.Format("{0};ServerTime:{1},Serverticks:{2},senderId:{3},target:{4}", data, now.ToString("yyyy-MM-dd HH:mm:ss.fff"), now.Ticks, connectionId, target);
        }

        void SaveReceiptTask()
        {
            while (true)
            {
                try
                {
                    if (_sendTask != null)
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < 100; i++)
                        {
                            var s = "";
                            _sendTask.TryDequeue(out s);
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                sb.AppendLine(s);
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (sb.Length > 0)
                        {
                            WriteFile(string.Format("{0}\r\n", sb.ToString()));
                        }
                        else
                        {
                            Thread.Sleep(10000);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
        int fileLength = 0;
        int fileIndex = 0;
        const int tenMb = 10485760;
        void WriteFile(string data)
        {
            lock (_syncObject)
            {
                if (fileLength >= tenMb)
                {
                    ++fileIndex;
                    fileLength = 0;
                }
                using (var file = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("log_{0}_{1}.log", t, fileIndex)), FileMode.Append, FileAccess.Write))
                {
                    var bytes = Encoding.Default.GetBytes(data);
                    fileLength += bytes.Length;
                    file.Write(bytes, 0, bytes.Length);
                    file.Flush();
                }
            }
        }
    }

    public enum ConnectionBehavior
    {
        ListenOnly,
        Echo,
        Broadcast
    }

    public enum TestMessageType
    {
        Single = 1,
        Group = 2,
        Receipt = 3
    }
    [Serializable]
    public class TestMessageData
    {
        public TestMessageType MessageType { get; set; }

        public string MessageId { get; set; }

        public string SenderId { get; set; }

        public string ClientTime { get; set; }

        public string ServerTime { get; set; }

        public long ServerTicks { get; set; }

        public string Content { get; set; }

        public string Target { get; set; }
    }
}