namespace Startup.Data
{
    // Gameplay depends on this contract, never on a concrete store (local now, server later).
    public interface IDataStore
    {
        void Save<T>(string key, T data);
        T Load<T>(string key, T defaultValue);
        bool Has(string key);
        void Delete(string key);
    }
}
