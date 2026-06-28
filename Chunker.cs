public class Chunk
{
    public string SourceFile { get; set; } = "";
    public string Department { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
}

public static class Chunker
{
    private const int TargetChunkSize = 500;   // approx words, not tokens (close enough for our scale)
    private const int OverlapWords = 60;

    public static List<Chunk> ChunkDocument(ExtractedDocument doc)
    {
        var paragraphs = doc.Content
            .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var chunks = new List<Chunk>();
        var currentWords = new List<string>();
        int chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphWords = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            currentWords.AddRange(paragraphWords);

            if (currentWords.Count >= TargetChunkSize)
            {
                chunks.Add(new Chunk
                {
                    SourceFile = doc.SourceFile,
                    Department = doc.Department,
                    ChunkIndex = chunkIndex++,
                    Content = string.Join(' ', currentWords)
                });

                // keep overlap words for the next chunk
                currentWords = currentWords
                    .Skip(Math.Max(0, currentWords.Count - OverlapWords))
                    .ToList();
            }
        }

        // flush remaining words as a final chunk
        if (currentWords.Count > 0)
        {
            chunks.Add(new Chunk
            {
                SourceFile = doc.SourceFile,
                Department = doc.Department,
                ChunkIndex = chunkIndex++,
                Content = string.Join(' ', currentWords)
            });
        }

        Console.WriteLine($"Chunked {doc.SourceFile} into {chunks.Count} chunks");
        return chunks;
    }
}
