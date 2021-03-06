﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Crank
{
    class DefaultConnectionFactory : IConnectionFactory
    {
        public Connection CreateConnection(string url)
        {
            return new Connection(url);
        }
        /// <summary>
        /// CreateConnection
        /// </summary>
        /// <param name="url"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public Connection CreateConnection(string url, string queryString)
        {
            return new Connection(url, queryString);
        }

        public Connection CreateConnection(string url, IDictionary<string, string> queryString)
        {
            return new Connection(url, queryString);
        }
    }
}
