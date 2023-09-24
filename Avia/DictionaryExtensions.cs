namespace dev.wimmesberger.avia.price.tracker.Avia;

public static class DictionaryExtensions {
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory) {
        if (dictionary.TryGetValue(key, out TValue value)) {
            return value;
        }
        value = factory.Invoke(key);
        dictionary.Add(key, value);
        return value;
    }
}
