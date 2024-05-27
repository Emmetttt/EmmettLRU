using NUnit.Framework;

namespace EmmettLRU.Tests;

public class LeastRecentlyUsedCacheStandardTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(Int32.MinValue)]
    public void LeastRecentlyUsedCache_InitialisedWithInvalidCapacity_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LeastRecentlyUsedCache<int, int>(capacity));
    }
    
    [TestCase(1)]
    [TestCase(1000)]
    public void LeastRecentlyUsedCache_InitialisedWithValidCapacity_DoesNotThrow(int capacity)
    {
        Assert.DoesNotThrow(() => new LeastRecentlyUsedCache<int, int>(capacity));
    }
    
    [Test]
    public void LeastRecentlyUsedCache_CacheSizeTooLarge_OutOfMemories()
    {
        Assert.Throws<OutOfMemoryException>(() => new LeastRecentlyUsedCache<int, int>(Int32.MaxValue));
    }

    [Test]
    public void LeastRecentlyUsedCache_PutNullKey_Throws()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        Assert.Throws<ArgumentNullException>(() => cache.Put(null, "myValue"));
    }

    [Test]
    public void LeastRecentlyUsedCache_PutNullValue_Throws()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        Assert.DoesNotThrow(() => cache.Put("myKey", null));
    }

    [Test]
    public void LeastRecentlyUsedCache_PutDuplicateValue_Throws()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        var key = "myKey";
        Assert.DoesNotThrow(() => cache.Put(key, "value1"));
        Assert.Throws<ArgumentException>(() => cache.Put(key, "value2"));
    }

    [Test]
    public void LeastRecentlyUsedCache_PutValue_CanSubsequentlyGetValue()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        var key = "myKey";
        var value = "myValue";
        
        cache.Put(key, value);
        Assert.True(cache.TryGet(key, out var storedValue));
        Assert.That(storedValue, Is.EqualTo(value));
    }

    [Test]
    public void LeastRecentlyUsedCache_TryGetWhenCacheDoesNotContainValue_ReturnsFalse()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        Assert.False(cache.TryGet("NonExistingKey", out _));
    }

    [Test]
    public void LeastRecentlyUsedCache_TryGetWhenCacheContainsNullValue_ReturnsTrueAndNullValue()
    {
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: 10);
        var key = "myKey";
        cache.Put(key, null);
        Assert.True(cache.TryGet(key, out var result));
        Assert.Null(result);
    }

    [Test]
    public void LeastRecentlyUsedCache_PutMultipleValues_WhenStillBelowCapacity_CanSubsequentlyGetAllValues()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<string, string>(capacity);
        var inputs = Enumerable
            .Range(0, capacity)
            .ToDictionary(i => $"key{i}", i => $"value{i}");

        foreach (var kvp in inputs)
        {
            cache.Put(kvp.Key, kvp.Value);
        }

        foreach (var kvp in inputs)
        {
            Assert.True(cache.TryGet(kvp.Key, out var result));
            Assert.That(result, Is.EqualTo(kvp.Value));
        }
    }

    [Test]
    public void LeastRecentlyUsedCache_PutMultipleValues_KeepsTrackOfSizeCorrectly()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity);
        var inputs = Enumerable
            .Range(start: 1, capacity * 2)
            .ToDictionary(i => i, i => $"value{i}");
        
        foreach (var kvp in inputs)
        {
            cache.Put(kvp.Key, kvp.Value);
            Assert.That(cache.CurrentSize, Is.EqualTo(Math.Min(kvp.Key, capacity)));
        }
    }

    [Test]
    public void LeastRecentlyUsedCache_PutMultipleValues_WhenExceedsCapacity_CanOnlyRetrieveMostRecentValues()
    {
        var capacity = 10;
        var excessEntries = 5;
        var cache = new LeastRecentlyUsedCache<string, string>(capacity: capacity);
        var inputs = Enumerable
            .Range(0, capacity + excessEntries)
            .ToDictionary(i => $"key{i}", i => $"value{i}");

        foreach (var kvp in inputs)
        {
            cache.Put(kvp.Key, kvp.Value);
        }

        // Since we don't read in this test, our cache is FIFO. Therefore, the first 5 entries have been evicted from
        // the cache
        foreach (var kvp in inputs.Take(excessEntries))
        {
            Assert.False(cache.TryGet(kvp.Key, out _));
        }

        foreach (var kvp in inputs.Skip(excessEntries))
        {
            Assert.True(cache.TryGet(kvp.Key, out var result));
            Assert.That(result, Is.EqualTo(kvp.Value));
        }
    }

    [Test]
    public void LeastRecentlyUsedCache_PutMultipleValues_ThenReadFirst_DoesNotEvictFirstItemOnSubsequentPut()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);
        var inputs = Enumerable
            .Range(0, capacity)
            .ToDictionary(i => i, i => $"value{i}");

        foreach (var kvp in inputs)
        {
            cache.Put(kvp.Key, kvp.Value);
        }
        
        // Now let's read the first 5 values again
        foreach (var kvp in inputs.Take(5))
        {
            Assert.True(cache.TryGet(kvp.Key, out _));
        }
        
        // And insert a random value
        cache.Put(101, "someRandomValue");
        
        // Then we expect our 6th value to be evicted
        var expectedEvictedKey = inputs.Skip(5).First().Key;
        Assert.That(cache.CurrentSize, Is.EqualTo(cache.Capacity));
        Assert.False(cache.TryGet(expectedEvictedKey, out _));
        Assert.True(cache.TryGet(101, out _));
        foreach (var kvp in inputs.Where(kvp => kvp.Key != expectedEvictedKey))
        {
            Assert.True(cache.TryGet(kvp.Key, out _));
        }
    }

    [Test]
    public void LeastRecentlyUsedCache_WhenReadAcrossMultipleThreads_DoesNotThrow()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);
        var inputs = Enumerable
            .Range(0, capacity)
            .ToDictionary(i => i, i => $"value{i}");

        foreach (var kvp in inputs)
        {
            cache.Put(kvp.Key, kvp.Value);
        }
        
        // If we read across many threads, we never throw an exception
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(10, 10);

        var allReadsSuccessful = Enumerable.Range(0, 10_000)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(i => cache.TryGet(i % capacity, out _))
            .Distinct()
            .Single();

        Assert.True(allReadsSuccessful);
    }

    [Test]
    public void LeastRecentlyUsedCache_WhenWrittenAcrossMultipleThreads_DoesNotThrow()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);
        
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(10, 10);

        var allReadsSuccessful = Enumerable.Range(0, 10_000)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(i =>
            {
                cache.Put(i, i.ToString());
                return true;
            })
            .Distinct()
            .Single();

        Assert.True(allReadsSuccessful);
    }

    [Test]
    public void LeastRecentlyUsedCache_WhenWrittenAcrossMultipleThreads_CapacityIsAlwaysConsistent()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);

        // Prepopulate the cache with 10 values
        foreach (var i in Enumerable.Range(0, capacity))
        {
            cache.Put(i, "myValue");
        }
        
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(10, 10);

        // Every subsequent call to Current Size should be equal to the Capacity
        var allQueriedCurrentSizes = Enumerable.Range(0, 10_000)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(i =>
            {
                cache.Put(i, i.ToString());
                return cache.CurrentSize;
            })
            .Distinct()
            .Single();

        Assert.That(allQueriedCurrentSizes, Is.EqualTo(capacity));
    }

    [Test]
    public void LeastRecentlyUsedCache_WhenReadAndWrittenAcrossMultipleThreads_DoesNotThrow()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);
        
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(10, 10);

        var allReadsSuccessful = Enumerable.Range(0, 10_000)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(i =>
            {
                if (i % 2 == 0)
                    cache.TryGet(i, out _);
                else
                    cache.Put(i, i.ToString());

                return true;
            })
            .Distinct()
            .Single();

        Assert.True(allReadsSuccessful);
    }

    [Test]
    public void LeastRecentlyUsedCache_WhenReadAndWrittenAcrossMultipleThreads_AlwaysReadSameValue_ValueDoesNotGetEvicted()
    {
        var capacity = 10;
        var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);
        var repeatedReadKey = 0;
        cache.Put(repeatedReadKey, "someValue");
        
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(10, 10);

        var allReadsSuccessful = Enumerable.Range(1, 10_000)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(i =>
            {
                cache.TryGet(repeatedReadKey, out _);
                cache.Put(i, i.ToString());

                return cache.TryGet(repeatedReadKey, out _);
            })
            .Distinct()
            .Single();

        Assert.True(allReadsSuccessful);
    }
}