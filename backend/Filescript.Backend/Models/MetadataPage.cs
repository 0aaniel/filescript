using System.Text.Json;
using System.Text;

namespace Filescript.Backend.Models
{
    /// <summary>
    /// One block’s worth of metadata.
    /// </summary>
    public class MetadataPage
    {
        // The chunk of JSON data for this page
        public string JsonChunk { get; set; }

        // The next metadata page block index, or -1 if none
        public int NextPageBlock { get; set; } = -1;
    }

    /// <summary>
    /// Utility methods to read/write a MetadataPage to a fixed-size block.
    /// </summary>
    public static class MetadataPageSerializer
    {
        public static byte[] Serialize(MetadataPage page, int blockSize)
        {
            var json = JsonSerializer.Serialize(page);
            byte[] raw = Encoding.UTF8.GetBytes(json);

            if (raw.Length > blockSize)
                throw new System.InvalidOperationException("Metadata page too large for one block!");

            // Zero-pad to blockSize
            byte[] buffer = new byte[blockSize];
            raw.CopyTo(buffer, 0);
            return buffer;
        }

        public static MetadataPage Deserialize(byte[] blockData)
        {
            // Trim trailing zeros
            int trimLen = blockData.Length;
            while (trimLen > 0 && blockData[trimLen - 1] == 0)
                trimLen--;

            if (trimLen == 0) 
                return new MetadataPage { JsonChunk = "", NextPageBlock = -1 };

            byte[] trimmed = new byte[trimLen];
            System.Array.Copy(blockData, trimmed, trimLen);

            string json = Encoding.UTF8.GetString(trimmed);
            return JsonSerializer.Deserialize<MetadataPage>(json);
        }
    }
}
