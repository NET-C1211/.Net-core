﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class DisplayValue : KernelCommandBase
    {
        public DisplayValue(FormattedValue formattedValue, string id = null)
        {
            FormattedValue = formattedValue;
            Id = id;
        }

        public FormattedValue FormattedValue { get; }
        public string Id { get; }
    }
}