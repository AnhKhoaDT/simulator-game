namespace Startup.Shared
{
    // Lets Core persist the chosen language without depending on the Data layer.
    public interface ILanguagePreference
    {
        string Load(string defaultCode);
        void Save(string code);
    }
}
