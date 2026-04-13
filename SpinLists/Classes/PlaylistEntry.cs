using Newtonsoft.Json;

namespace SpinLists.Classes;

public class PlaylistEntry
{
    [JsonProperty(PropertyName = "fileReference")]
    public string FileReference = string.Empty;
    
    [JsonProperty(PropertyName = "title")]
    public string Title = string.Empty;
    
    [JsonProperty(PropertyName = "artist")]
    public string Artist = string.Empty;
    
    [JsonProperty(PropertyName = "charter")]
    public string Charter = string.Empty;

    [JsonConstructor]
    public PlaylistEntry() { }
    
    public PlaylistEntry(MetadataHandle metadataHandle)
    {
        FileReference = Utils.GetFileReference(metadataHandle);
        Title = metadataHandle.trackInfoMetadata.title;
        Artist = metadataHandle.trackInfoMetadata.artistName;
        Charter = metadataHandle.trackInfoMetadata.charter;
    }
}