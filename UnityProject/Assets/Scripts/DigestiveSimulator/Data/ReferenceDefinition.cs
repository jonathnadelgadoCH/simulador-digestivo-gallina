using System;

namespace DigestiveSimulator.Data
{
    [Serializable]
    public sealed class ReferenceLibrary
    {
        public string schemaVersion;
        public string contentVersion;
        public ReferenceDefinition[] references;
    }

    [Serializable]
    public sealed class ReferenceDefinition
    {
        public string id;
        public string authors;
        public string title;
        public int year;
        public string journal;
        public string book;
        public string edition;
        public string volume;
        public string issue;
        public string pages;
        public string doi;
        public string url;
    }
}
