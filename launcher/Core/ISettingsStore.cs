namespace BlackLauncher
{
    public interface ISettingsStore
    {
        string Get(string key);
        void Set(string key, string value);
        bool GetBool(string key);
        void SetBool(string key, bool value);
        double GetNumber(string key);
        void SetNumber(string key, double value, string format);
    }
}
