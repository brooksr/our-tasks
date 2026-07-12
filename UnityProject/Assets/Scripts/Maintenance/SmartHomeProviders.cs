using System.Collections;
using System.Collections.Generic;

namespace OurTasks.Maintenance
{
    public sealed class DeviceState
    {
        public bool available;
        public string status;
        public Dictionary<string, object> values = new();
    }

    public interface ISmartHomeProvider
    {
        string Id { get; }
        IEnumerator GetDeviceState(string deviceId, System.Action<DeviceState> callback);
        IEnumerator ExecuteCommand(string deviceId, string command, object value, System.Action<bool> callback);
    }

    public sealed class MockSmartHomeProvider : ISmartHomeProvider
    {
        public string Id => "mock";
        public IEnumerator GetDeviceState(string deviceId, System.Action<DeviceState> callback)
        {
            callback(new DeviceState { available = false, status = "Mock device — no live provider configured" });
            yield break;
        }
        public IEnumerator ExecuteCommand(string deviceId, string command, object value, System.Action<bool> callback)
        {
            callback(false);
            yield break;
        }
    }

    public static class SmartHomeProviderRegistry
    {
        static readonly Dictionary<string, ISmartHomeProvider> Providers = new() { ["mock"] = new MockSmartHomeProvider() };
        public static void Register(ISmartHomeProvider provider) => Providers[provider.Id] = provider;
        public static bool TryGet(string id, out ISmartHomeProvider provider) => Providers.TryGetValue(id, out provider);
    }
}
