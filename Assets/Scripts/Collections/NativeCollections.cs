using Unity.Collections;
using Unity.Mathematics;
using System;

namespace uEmuera.Collections
{
    /// <summary>
    /// High-performance object pool using NativeList for efficient memory management.
    /// This pool is designed for value types and blittable structs.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool. Must be an unmanaged type.</typeparam>
    public struct NativePool<T> : IDisposable where T : unmanaged
    {
        private NativeList<T> pool_;
        private NativeList<bool> inUse_;
        private int capacity_;
        private Allocator allocator_;

        /// <summary>
        /// Creates a new native pool with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the pool.</param>
        /// <param name="allocator">Memory allocator to use.</param>
        public NativePool(int initialCapacity, Allocator allocator)
        {
            capacity_ = initialCapacity;
            allocator_ = allocator;
            pool_ = new NativeList<T>(initialCapacity, allocator);
            inUse_ = new NativeList<bool>(initialCapacity, allocator);
            
            // Initialize all slots as available
            for (int i = 0; i < initialCapacity; i++)
            {
                pool_.Add(default);
                inUse_.Add(false);
            }
        }

        /// <summary>
        /// Gets the current capacity of the pool.
        /// </summary>
        public int Capacity => capacity_;

        /// <summary>
        /// Gets the number of objects currently in use.
        /// </summary>
        public int InUseCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < inUse_.Length; i++)
                {
                    if (inUse_[i]) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the number of available objects in the pool.
        /// </summary>
        public int AvailableCount => capacity_ - InUseCount;

        /// <summary>
        /// Acquires an object from the pool.
        /// </summary>
        /// <param name="index">Output parameter containing the index of the acquired object.</param>
        /// <param name="item">Output parameter containing the acquired object.</param>
        /// <returns>True if an object was successfully acquired.</returns>
        public bool TryAcquire(out int index, out T item)
        {
            // Find first available slot
            for (int i = 0; i < capacity_; i++)
            {
                if (!inUse_[i])
                {
                    inUse_[i] = true;
                    index = i;
                    item = pool_[i];
                    return true;
                }
            }

            // No available slots, try to expand
            if (TryExpand())
            {
                index = capacity_ - 1;
                inUse_[index] = true;
                item = pool_[index];
                return true;
            }

            index = -1;
            item = default;
            return false;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="index">The index of the object to return.</param>
        /// <param name="item">The object to return to the pool.</param>
        public void Release(int index, T item)
        {
            if (index < 0 || index >= capacity_)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            pool_[index] = item;
            inUse_[index] = false;
        }

        /// <summary>
        /// Clears all objects from the pool and marks them as available.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < capacity_; i++)
            {
                pool_[i] = default;
                inUse_[i] = false;
            }
        }

        /// <summary>
        /// Attempts to expand the pool by doubling its capacity.
        /// </summary>
        /// <returns>True if expansion was successful.</returns>
        private bool TryExpand()
        {
            int newCapacity = math.min(capacity_ * 2, capacity_ + 1000); // Limit growth
            if (newCapacity <= capacity_)
                return false;

            int itemsToAdd = newCapacity - capacity_;
            for (int i = 0; i < itemsToAdd; i++)
            {
                pool_.Add(default);
                inUse_.Add(false);
            }

            capacity_ = newCapacity;
            return true;
        }

        /// <summary>
        /// Disposes the native pool and frees its memory.
        /// </summary>
        public void Dispose()
        {
            if (pool_.IsCreated)
                pool_.Dispose();
            if (inUse_.IsCreated)
                inUse_.Dispose();
        }

        /// <summary>
        /// Gets whether the pool has been created and not yet disposed.
        /// </summary>
        public bool IsCreated => pool_.IsCreated && inUse_.IsCreated;
    }

    /// <summary>
    /// Ring buffer implementation using NativeArray for efficient FIFO operations.
    /// Useful for frame history, input buffering, etc.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer. Must be an unmanaged type.</typeparam>
    public struct NativeRingBuffer<T> : IDisposable where T : unmanaged
    {
        private NativeArray<T> buffer_;
        private int head_;
        private int tail_;
        private int count_;
        private int capacity_;
        private Allocator allocator_;

        /// <summary>
        /// Creates a new ring buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of elements the buffer can hold.</param>
        /// <param name="allocator">Memory allocator to use.</param>
        public NativeRingBuffer(int capacity, Allocator allocator)
        {
            capacity_ = capacity;
            allocator_ = allocator;
            buffer_ = new NativeArray<T>(capacity, allocator);
            head_ = 0;
            tail_ = 0;
            count_ = 0;
        }

        /// <summary>
        /// Gets the maximum capacity of the buffer.
        /// </summary>
        public int Capacity => capacity_;

        /// <summary>
        /// Gets the current number of elements in the buffer.
        /// </summary>
        public int Count => count_;

        /// <summary>
        /// Gets whether the buffer is empty.
        /// </summary>
        public bool IsEmpty => count_ == 0;

        /// <summary>
        /// Gets whether the buffer is full.
        /// </summary>
        public bool IsFull => count_ == capacity_;

        /// <summary>
        /// Adds an element to the end of the buffer.
        /// If the buffer is full, the oldest element is overwritten.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Enqueue(T item)
        {
            buffer_[tail_] = item;
            tail_ = (tail_ + 1) % capacity_;

            if (count_ < capacity_)
            {
                count_++;
            }
            else
            {
                // Buffer is full, move head forward (overwrite oldest)
                head_ = (head_ + 1) % capacity_;
            }
        }

        /// <summary>
        /// Attempts to remove and return the element at the beginning of the buffer.
        /// </summary>
        /// <param name="item">The removed item, or default if buffer is empty.</param>
        /// <returns>True if an item was successfully dequeued.</returns>
        public bool TryDequeue(out T item)
        {
            if (count_ == 0)
            {
                item = default;
                return false;
            }

            item = buffer_[head_];
            head_ = (head_ + 1) % capacity_;
            count_--;
            return true;
        }

        /// <summary>
        /// Attempts to peek at the element at the beginning of the buffer without removing it.
        /// </summary>
        /// <param name="item">The peeked item, or default if buffer is empty.</param>
        /// <returns>True if an item was successfully peeked.</returns>
        public bool TryPeek(out T item)
        {
            if (count_ == 0)
            {
                item = default;
                return false;
            }

            item = buffer_[head_];
            return true;
        }

        /// <summary>
        /// Gets an element at the specified index (0 = oldest, Count-1 = newest).
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count_)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int actualIndex = (head_ + index) % capacity_;
                return buffer_[actualIndex];
            }
        }

        /// <summary>
        /// Clears all elements from the buffer.
        /// </summary>
        public void Clear()
        {
            head_ = 0;
            tail_ = 0;
            count_ = 0;
        }

        /// <summary>
        /// Disposes the ring buffer and frees its memory.
        /// </summary>
        public void Dispose()
        {
            if (buffer_.IsCreated)
                buffer_.Dispose();
        }

        /// <summary>
        /// Gets whether the buffer has been created and not yet disposed.
        /// </summary>
        public bool IsCreated => buffer_.IsCreated;
    }
}
