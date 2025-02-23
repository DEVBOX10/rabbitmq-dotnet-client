// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 2.0.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2020 VMware, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       https://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v2.0:
//
//---------------------------------------------------------------------------
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
//  Copyright (c) 2007-2020 VMware, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Xunit;

namespace RabbitMQ.Client.Unit
{
    [Collection("IntegrationFixture")]
    public class TestConsumer
    {
        [Fact]
        public async Task TestBasicRoundtripConcurrent()
        {
            var cf = new ConnectionFactory { ConsumerDispatchConcurrency = 2 };
            using (IConnection c = cf.CreateConnection())
            using (IChannel m = c.CreateChannel())
            {
                QueueDeclareOk q = m.QueueDeclare();
                const string publish1 = "sync-hi-1";
                byte[] body = Encoding.UTF8.GetBytes(publish1);
                m.BasicPublish("", q.QueueName, body);
                const string publish2 = "sync-hi-2";
                body = Encoding.UTF8.GetBytes(publish2);
                m.BasicPublish("", q.QueueName, body);

                var consumer = new EventingBasicConsumer(m);

                var publish1SyncSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var publish2SyncSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var maximumWaitTime = TimeSpan.FromSeconds(10);
                var tokenSource = new CancellationTokenSource(maximumWaitTime);
                tokenSource.Token.Register(() =>
                {
                    publish1SyncSource.TrySetResult(false);
                    publish2SyncSource.TrySetResult(false);
                });

                consumer.Received += (o, a) =>
                {
                    switch (Encoding.UTF8.GetString(a.Body.ToArray()))
                    {
                        case publish1:
                            publish1SyncSource.TrySetResult(true);
                            break;
                        case publish2:
                            publish2SyncSource.TrySetResult(true);
                            break;
                    }
                };

                m.BasicConsume(q.QueueName, true, consumer);

                await Task.WhenAll(publish1SyncSource.Task, publish2SyncSource.Task);

                bool result1 = await publish1SyncSource.Task;
                Assert.True(result1, $"Non concurrent dispatch lead to deadlock after {maximumWaitTime}");

                bool result2 = await publish1SyncSource.Task;
                Assert.True(result2, $"Non concurrent dispatch lead to deadlock after {maximumWaitTime}");
            }
        }
    }
}
