using System.Diagnostics.Contracts;
using System.Text.Json;

namespace dnd_lang.@in
{
    public class Action
    {
        public string name { get; set; }
        public JsonElement text { get; set; }
        public JsonElement attack { get; set; }
    }
}