﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit.Extensions;
using Xunit.Sdk;

namespace Microsoft.TestCommon
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WsrTheoryAttribute : TheoryAttribute
    {
        public WsrTheoryAttribute()
        {
            Timeout = TimeoutConstant.DefaultTimeout;
            Platforms = Platform.All;
            PlatformJustification = "Unsupported platform (test runs on {0}, current platform is {1})";
        }

        /// <summary>
        /// Gets the platform that the unit test is currently running on.
        /// </summary>
        protected Platform Platform
        {
            get { return PlatformInfo.Platform; }
        }

        /// <summary>
        /// Gets or set the platforms that the unit test is compatible with. Defaults to
        /// <see cref="Platform.All"/>.
        /// </summary>
        public Platform Platforms { get; set; }

        /// <summary>
        /// Gets or sets the platform skipping justification. This message can receive
        /// the supported platforms as {0}, and the current platform as {1}.
        /// </summary>
        public string PlatformJustification { get; set; }

        /// <inheritdoc/>
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            if ((Platforms & Platform) == 0)
            {
                return new[] {
                    new SkipCommand(
                        method,
                        DisplayName,
                        String.Format(PlatformJustification, Platforms.ToString().Replace(", ", " | "), Platform)
                    )
                };
            }

            return base.EnumerateTestCommands(method);
        }
    }
}
