using NUnit.Framework;
using CrystalFrost.Lib;

namespace CrystalFrostEngine.Tests
{
    public class AbstractedConcurrentQueue_Test
    {
        [Test]
        public void EnqueueAndDequeue_WorksCorrectly()
        {
            // Arrange
            var queue = new AbstractedConcurrentQueue<int>();
            int itemToEnqueue = 42;

            // Act
            queue.Enqueue(itemToEnqueue);
            bool result = queue.TryDequeue(out int dequeuedItem);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(itemToEnqueue, dequeuedItem);
        }

        [Test]
        public void Count_IsCorrect()
        {
            // Arrange
            var queue = new AbstractedConcurrentQueue<string>();

            // Assert initial state
            Assert.AreEqual(0, queue.Count);

            // Act & Assert after Enqueue
            queue.Enqueue("hello");
            Assert.AreEqual(1, queue.Count);

            // Act & Assert after Dequeue
            queue.TryDequeue(out _);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void TryDequeue_OnEmptyQueue_ReturnsFalse()
        {
            // Arrange
            var queue = new AbstractedConcurrentQueue<float>();

            // Act
            bool result = queue.TryDequeue(out _);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ItemEnqueued_EventIsFired()
        {
            // Arrange
            var queue = new AbstractedConcurrentQueue<int>();
            int enqueuedItem = 0;
            queue.ItemEnqueued += (item) => { enqueuedItem = item; };
            int itemToEnqueue = 123;

            // Act
            queue.Enqueue(itemToEnqueue);

            // Assert
            Assert.AreEqual(itemToEnqueue, enqueuedItem);
        }

        [Test]
        public void ItemDequeued_EventIsFired()
        {
            // Arrange
            var queue = new AbstractedConcurrentQueue<int>();
            int dequeuedItemFromEvent = 0;
            queue.ItemDequeued += (item) => { dequeuedItemFromEvent = item; };
            int itemToEnqueue = 456;
            queue.Enqueue(itemToEnqueue);

            // Act
            queue.TryDequeue(out int _);

            // Assert
            Assert.AreEqual(itemToEnqueue, dequeuedItemFromEvent);
        }
    }
}
