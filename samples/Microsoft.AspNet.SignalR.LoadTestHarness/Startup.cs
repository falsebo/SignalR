// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.LoadTestHarness;
using Microsoft.Owin;
using Owin;
using System.Configuration;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Redis;
[assembly: OwinStartup(typeof(Startup))]

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(60);

            var connectionString = ConfigurationManager.AppSettings.Get("redis:connectionString");
            var eventKey = ConfigurationManager.AppSettings.Get("redis:eventKey");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                GlobalHost.DependencyResolver.UseRedis(new RedisScaleoutConfiguration(connectionString, eventKey));
            }

            app.MapSignalR<TestConnection>("/TestConnection");
            app.MapSignalR();

            Dashboard.Init();
        }
    }
}