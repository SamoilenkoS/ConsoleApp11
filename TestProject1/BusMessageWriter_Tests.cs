using BusMessageLibrary;
using FluentAssertions;
using Moq;
using System.Text;

namespace TestProject1
{
    public class Tests
    {
        private BusMessageWriter _messageWriter;
        private Mock<IBusConnection> _busConnection;

        [SetUp]
        public void Setup()
        {
            _busConnection = new Mock<IBusConnection>();
            _messageWriter = new BusMessageWriter(_busConnection.Object);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        public async Task ShouldPublishBufferDataToBusConnection_WhenBufferFilled(int threadsCount)
        {
            int itemsPerThread = Constants.BufferThreshold / threadsCount;
            Random random = new Random();
            _busConnection.Setup(x =>
                x.PublishAsync(It.Is<byte[]>(byteArray => ValidateByteArray(
                    byteArray,
                    threadsCount,
                    itemsPerThread))))
                .Verifiable("Byte array should be published to bus connection!");

            await Parallel.ForEachAsync(
                Enumerable.Repeat(0, threadsCount)
                .Select(x => random.Next(10)),//limited to single digit items generation to be sure that it will always have same size of generated array
                new ParallelOptions { MaxDegreeOfParallelism = threadsCount },
                async (item, _) =>
                {
                    byte[] bytes = GenerateByteArray(item, itemsPerThread);
                    await _messageWriter.SendMessageAsync(bytes);
                });

            _busConnection.Verify();
        }

        private static byte[] GenerateByteArray(
            int item,
            int itemsPerThread)
        {
            return Encoding.ASCII.GetBytes(
                string.Join(
                    string.Empty,
                    Enumerable.Repeat(item, itemsPerThread)));
        }

        private static bool ValidateByteArray(
            byte[] array,
            int threadsCount,
            int itemsPerThread)
        {
            if (array == null || array.Length < Constants.BufferThreshold)
            {
                Assert.Fail("Buffer wasn't filled!");
            }

            for (int i = 0; i < threadsCount; i++)
            {
                var subArray = array
                    !.Skip(i * itemsPerThread)
                    .Take(itemsPerThread)
                    .ToArray();
                subArray
                    .Should()
                    .BeEquivalentTo(Enumerable.Repeat(subArray[0], itemsPerThread));
            }

            return true;
        }
    }
}