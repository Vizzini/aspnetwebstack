﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using Xunit;
using Assert = Microsoft.TestCommon.AssertEx;

namespace System.Net.Http
{
    public class HttpUnsortedResponseTest
    {
        [Fact]
        public void Constructor_InitializesHeaders()
        {
            HttpUnsortedResponse response = new HttpUnsortedResponse();
            Assert.IsType<HttpUnsortedHeaders>(response.HttpHeaders);
        }
    }
}
