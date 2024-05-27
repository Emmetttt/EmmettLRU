namespace EmmettLRU;

public interface IBoundedCache<K, V> where K : notnull, IEquatable<K>
{
    public bool TryGet(K key, out V value);

    public void Put(K key, V value);

    public int CurrentSize { get; }
    
    public int Capacity { get; }
}