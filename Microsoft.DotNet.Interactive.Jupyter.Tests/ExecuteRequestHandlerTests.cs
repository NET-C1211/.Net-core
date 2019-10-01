﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using Envelope = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class ExecuteRequestHandlerTests : JupyterRequestHandlerTestBase<ExecuteRequest>
    {
        public ExecuteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task sends_ExecuteInput_when_ExecuteRequest_is_handled()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("var a =12;"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.PubSubMessages.Should()
                .ContainItemsAssignableTo<ExecuteInput>();

            JupyterMessageSender.PubSubMessages.OfType<ExecuteInput>().Should().Contain(r => r.Code == "var a =12;");
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_when_code_submission_is_handled()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("var a =12;"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                .Should()
                .ContainItemsAssignableTo<ExecuteReplyOk>();
        }

        [Fact]
        public async Task sends_ExecuteReply_with_error_message_on_when_code_submission_contains_errors()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("asdes"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyError>();
        }

        [Fact]
        public async Task sends_DisplayData_message_on_ValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("Console.WriteLine(2+2);"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is DisplayData);
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_ReturnValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("2+2"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is ExecuteResult);
        }

        [Fact]
        public async Task sends_ExecuteReply_message_when_submission_contains_only_a_directive()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("%%csharp"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyOk>();
        }
    }
}
