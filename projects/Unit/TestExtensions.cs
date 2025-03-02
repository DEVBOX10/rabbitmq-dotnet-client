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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RabbitMQ.Client.Unit
{
    public class TestExtensions : IntegrationFixture
    {
        public TestExtensions(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task TestConfirmBarrier()
        {
            _channel.ConfirmSelect();
            for (int i = 0; i < 10; i++)
            {
                _channel.BasicPublish("", string.Empty);
            }
            Assert.True(await _channel.WaitForConfirmsAsync());
        }

        [Fact]
        public async Task TestConfirmBeforeWait()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _channel.WaitForConfirmsAsync());
        }

        [Fact]
        public async Task TestExchangeBinding()
        {
            _channel.ConfirmSelect();

            _channel.ExchangeDeclare("src", ExchangeType.Direct, false, false, null);
            _channel.ExchangeDeclare("dest", ExchangeType.Direct, false, false, null);
            string queue = _channel.QueueDeclare();

            _channel.ExchangeBind("dest", "src", string.Empty);
            _channel.ExchangeBind("dest", "src", string.Empty);
            _channel.QueueBind(queue, "dest", string.Empty);

            _channel.BasicPublish("src", string.Empty);
            await _channel.WaitForConfirmsAsync();
            Assert.NotNull(_channel.BasicGet(queue, true));

            _channel.ExchangeUnbind("dest", "src", string.Empty);
            _channel.BasicPublish("src", string.Empty);
            await _channel.WaitForConfirmsAsync();
            Assert.Null(_channel.BasicGet(queue, true));

            _channel.ExchangeDelete("src");
            _channel.ExchangeDelete("dest");
        }
    }
}
