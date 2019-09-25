// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class MessageDispatcher
    {
        private readonly IPubSubChannel _pubSubChannel;
        private readonly IReplyChannel _replyChannel;
        private readonly string _kernelIdent;

        public MessageDispatcher(IPubSubChannel pubSubChannel, IReplyChannel replyChannel, string kernelIdent)
        {
            if (string.IsNullOrWhiteSpace(kernelIdent))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(kernelIdent));
            }

            _pubSubChannel = pubSubChannel ?? throw new ArgumentNullException(nameof(pubSubChannel));
            _replyChannel = replyChannel ?? throw new ArgumentNullException(nameof(replyChannel));
            _kernelIdent = kernelIdent;
        }

        public void Dispatch(JupyterMessageContent messageContent, Message request)
        {
            switch (messageContent)
            {
                case JupyterPubSubMessageContent pubSubMessageContent:
                    _pubSubChannel.Publish(pubSubMessageContent, request, _kernelIdent);
                    break;
                case JupyterReplyMessageContent replyMessageContent:
                    _replyChannel.Reply(replyMessageContent, request);
                    break;
            }
        }
    }
}