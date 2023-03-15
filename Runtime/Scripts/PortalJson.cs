using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public interface PortalJsonLike
{
    public string Version { get; set; }
}

[System.Serializable]
public class PortalJson : PortalJsonLike
{
    [System.Serializable]
    public class _Presentation
    {
        [JsonProperty("3d_model_url")]
        public string ModelUrl { get; set; }

        [JsonProperty("panorama_url")]
        public string PanoramaUrl { get; set; }
    }

    [System.Serializable]
    public class _Destination
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("presentation")]
    public _Presentation Presentation { get; set; }

    [JsonProperty("destination")]
    public _Destination Destination { get; set; }


    public static PortalJson CreateFromJSON(string jsonString)
    {
        return JsonConvert.DeserializeObject<PortalJson>(jsonString);
    }
}
