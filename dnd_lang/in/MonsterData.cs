using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dnd_lang.@in
{
    public class MonsterData
    {
        public string name { get; set; }
        public JsonElement trait { get; set; }
        public JsonElement action { get; set; }
    }
}