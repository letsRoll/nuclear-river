using System;
using System.IO;

using Microsoft.ServiceBus.Messaging;

using NuClear.OperationsLogging.Transports.ServiceBus;
using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports.ServiceBus
{
    public sealed class BinaryEvent2BrokeredMessageConverter : IEvent2BrokeredMessageConverter<IEvent>
    {
        private readonly IBinaryEventSerializer _serializer;

        public BinaryEvent2BrokeredMessageConverter(IBinaryEventSerializer serializer)
        {
            _serializer = serializer;
        }

        public BrokeredMessage Convert(IEvent @event)
        {
            var stream = new MemoryStream();
            var writer = new StreamBinaryWriter(stream);
            _serializer.Serialize(writer, @event);
            return new BrokeredMessage(stream, true);
        }

        private class StreamBinaryWriter : IBinaryWriter
        {
            private readonly Stream _stream;

            public StreamBinaryWriter(Stream stream)
            {
                _stream = stream;
            }

            public void Write(byte[] buffer)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                Write(buffer, 0, buffer.Length);
            }

            public void Write(byte[] buffer, int index, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                _stream.Write(buffer, index, count);
            }
        }
    }
}