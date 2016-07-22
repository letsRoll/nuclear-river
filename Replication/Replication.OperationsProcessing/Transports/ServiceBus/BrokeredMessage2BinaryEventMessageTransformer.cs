using System;
using System.IO;

using Microsoft.ServiceBus.Messaging;

using NuClear.Messaging.API.Processing.Actors.Transformers;
using NuClear.OperationsProcessing.Transports.ServiceBus;
using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports.ServiceBus
{
    public sealed class BrokeredMessage2BinaryEventMessageTransformer : MessageTransformerBase<BrokeredMessageDecorator, EventMessage>
    {
        private readonly IBinaryEventSerializer _eventSerializer;

        public BrokeredMessage2BinaryEventMessageTransformer(IBinaryEventSerializer eventSerializer)
        {
            _eventSerializer = eventSerializer;
        }

        protected override EventMessage Transform(BrokeredMessageDecorator originalMessage)
        {
            return new EventMessage(originalMessage.Id, Transform(originalMessage.Message));
        }

        private IEvent Transform(BrokeredMessage message)
        {
            var bodyStream = message.GetBody<Stream>();
            var binaryReader = new StreamBinaryReader(bodyStream);
            return _eventSerializer.Deserialize(binaryReader);
        }

        private class StreamBinaryReader : IBinaryReader
        {
            private const int Capacity = 1024;
            private readonly Stream _stream;

            public StreamBinaryReader(Stream stream)
            {
                _stream = stream;
            }

            public int Read(byte[] buffer, int index, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                return _stream.Read(buffer, index, count);
            }

            public byte[] ReadToEnd()
            {
                using (var memory = new MemoryStream(Capacity))
                {
                    _stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }
    }
}