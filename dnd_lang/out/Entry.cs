using System.Text.Json.Serialization;

namespace dnd_lang.@out
{
    public class Entry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }

        [JsonIgnore] 
        public bool localized { get; set; } = false;
    }
}