namespace AbzorbaExportRoot.Editor.ArtificeToolkit
{
    public interface IArtificePersistence
    {
        public string ViewPersistenceKey { get; set; }

        public void SavePersistedData();
        public void LoadPersistedData();
    }
}
