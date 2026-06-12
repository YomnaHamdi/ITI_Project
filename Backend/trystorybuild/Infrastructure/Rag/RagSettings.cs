namespace Infrastructure.Rag
{
    public class RagSettings
    {
        public string OllamaEndpoint     { get; set; } = "http://localhost:11434";
        public string EmbeddingModel     { get; set; } = "nomic-embed-text";
        public string VisionModel        { get; set; } = "minicpm-v";
        public string ChromaEndpoint     { get; set; } = "http://localhost:8001";
        public string CollectionName     { get; set; } = "arabic_lessons";
        public string DocumentsDirectory { get; set; } = "Uploads/Rag";
        public int    ChunkSize          { get; set; } = 400;
        public int    ChunkOverlap       { get; set; } = 60;
    }
}
