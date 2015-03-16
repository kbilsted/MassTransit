﻿// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.RabbitMqTransport.Tests
{
    namespace Send_Specs
    {
        using System;
        using System.Linq;
        using System.Threading.Tasks;
        using Configuration;
        using NUnit.Framework;
        using Serialization;
        using TestFramework;


        [TestFixture]
        public class WhenAMessageIsSendToTheEndpoint :
            RabbitMqTestFixture
        {
            [Test]
            public async void Should_be_received()
            {
                ISendEndpoint endpoint = await Bus.GetSendEndpoint(InputQueueAddress);

                var message = new A {Id = Guid.NewGuid()};
                await endpoint.Send(message);

                ConsumeContext<A> received = await _receivedA;

                Assert.AreEqual(message.Id, received.Message.Id);
            }

            Task<ConsumeContext<A>> _receivedA;

            protected override void ConfigureInputQueueEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
            {
                base.ConfigureInputQueueEndpoint(configurator);

                _receivedA = Handler<A>(configurator);
            }
        }


        [TestFixture]
        public class WhenAMessageIsSendToTheEndpointEncrypted :
            RabbitMqTestFixture
        {
            [Test]
            public async void Should_be_received()
            {
                ISendEndpoint endpoint = await Bus.GetSendEndpoint(InputQueueAddress);

                var message = new A {Id = Guid.NewGuid()};
                await endpoint.Send(message);

                ConsumeContext<A> received = await _receivedA;

                Assert.AreEqual(message.Id, received.Message.Id);

                Assert.AreEqual(EncryptedMessageSerializer.EncryptedContentType, received.ReceiveContext.ContentType);
            }

            Task<ConsumeContext<A>> _receivedA;

            protected override void ConfigureInputQueueEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
            {
                base.ConfigureInputQueueEndpoint(configurator);

                _receivedA = Handler<A>(configurator);
            }

            protected override void ConfigureBus(IRabbitMqBusFactoryConfigurator configurator)
            {
                ISymmetricKeyProvider keyProvider = new TestSymmetricKeyProvider();
                var streamProvider = new AesCryptoStreamProvider(keyProvider, "default");
                configurator.UseEncryptedSerializer(streamProvider);

                base.ConfigureBus(configurator);
            }
        }


        [TestFixture]
        public class WhenAMessageIsPublishedToTheEndpoint :
            RabbitMqTestFixture
        {
            [Test]
            public async void Should_be_received()
            {
                var message = new A {Id = Guid.NewGuid()};
                await Bus.Publish(message);

                ConsumeContext<A> received = await _receivedA;

                Assert.AreEqual(message.Id, received.Message.Id);
            }

            Task<ConsumeContext<A>> _receivedA;

            protected override void ConfigureInputQueueEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
            {
                base.ConfigureInputQueueEndpoint(configurator);

                _receivedA = Handler<A>(configurator);
            }
        }

        [TestFixture]
        public class WhenAMessageIsPublishedToTheConsumer :
            RabbitMqTestFixture
        {
            [Test]
            public async void Should_be_received()
            {
                var message = new B {Id = Guid.NewGuid()};
                await Bus.Publish(message);

                var received = ConsumerOf<B>.AnyShouldHaveReceivedMessage(message, TestTimeout).ToList();

                Assert.AreEqual(message.Id, received[0].Message.Id);
            }

            protected override void ConfigureInputQueueEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
            {
                base.ConfigureInputQueueEndpoint(configurator);

                configurator.Consumer<ConsumerOf<B>>();
            }
        }


        [TestFixture]
        public class When_a_message_is_published_without_a_queue_binding :
            RabbitMqTestFixture
        {
            [Test]
            public async void Should_not_throw_an_exception()
            {
                var message = new UnboundMessage {Id = Guid.NewGuid()};

                await Bus.Publish(message);
            }


            class UnboundMessage
            {
                public Guid Id { get; set; }
            }
        }


        class A
        {
            public Guid Id { get; set; }
        }

        class B : IEquatable<B>
        {
            public Guid Id { get; set; }

            public bool Equals(B other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return Id.Equals(other.Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((B)obj);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}