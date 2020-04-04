using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using dnd_lang.lang;
using Yandex.Translator;
using Type = dnd_lang.lang.Type;

namespace dnd_lang
{
    class Program
    {
        private static Entry FindInDictionary(List<Entry> dictionary, string search)
        {
            var r = dictionary.FirstOrDefault(x => x.Key == search);
            if (r != null)
                return r;
            
            r = dictionary.FirstOrDefault(x => search.StartsWith(x.Key));
            if (r != null)
                return r;

            return null;
        }
        
        private static void LocalizeEntriesThrowYandex(List<@out.Entry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.localized)
                    continue;
                
                if (m_yandex_translator == null)
                    m_yandex_translator = Yandex.Translator.Yandex.Translator(api =>
                        api.ApiKey(m_yandex_api_key)
                            .Format(ApiDataFormat.Json));

                var origin = entry.name.Trim();
                var translation = m_yandex_translator.Translate("en-ru", origin);
                
                if (translation == null)
                    continue;

                var translated = translation.Text.Trim();
                entry.name = $"{translated} ({origin})";
                entry.localized = true;
                
                Console.WriteLine($"{origin} = {translated}");
            }
        }

        private static IYandexTranslator m_yandex_translator;
        private static string m_yandex_api_key;

        static void Main(string[] args)
        {
            if (args.Length < 2)
                return;

            var outPath = args[0]; //First arg is output path (see files saving below)
            m_yandex_api_key = args[1]; //Yandex Translate api key (https://yandex.ru/dev/keys/?service=trnsl)
            
            var if0 = File.ReadAllBytes(@"../../../../in-ru/DnD5e_spells_BD.dtn");
            var iu0 = new Utf8JsonReader(if0);
            var spellsIn = JsonSerializer.Deserialize<@in.SpellsDB>(ref iu0);

            var if1 = File.ReadAllBytes(@"../../../../in-ru/DnD5e_equipment_BD.dtn");
            var iu1 = new Utf8JsonReader(if1);
            var itemsIn = JsonSerializer.Deserialize<@in.ItemsDB>(ref iu1);

            var if2 = File.ReadAllBytes(@"../../../../in-ru//DnD5e_equipment_BD-2.dtn");
            var iu2 = new Utf8JsonReader(if2);
            var items2In = JsonSerializer.Deserialize<@in.ItemsDB>(ref iu2);

            var if3 = File.ReadAllBytes(@"../../../../in-ru//DnD5e_monsters_BD.dtn");
            var iu3 = new Utf8JsonReader(if3);
            var monstersIn = JsonSerializer.Deserialize<@in.MonsterDB>(ref iu3);
            
            //---------

            var of0 = File.ReadAllBytes(@"../../../../out-en/dnd5e.spells.json");
            var ou0 = new Utf8JsonReader(of0);
            var spellsOut = JsonSerializer.Deserialize<@out.SpellsDB>(ref ou0);

            var of1 = File.ReadAllBytes(@"../../../../out-en/dnd5e.items.json");
            var ou1 = new Utf8JsonReader(of1);
            var itemsOut = JsonSerializer.Deserialize<@out.ItemsDB>(ref ou1);

            var of2 = File.ReadAllBytes(@"../../../../out-en/dnd5e.monsters.json");
            var ou2 = new Utf8JsonReader(of2);
            var monsterOut = JsonSerializer.Deserialize<@out.MonsterDB>(ref ou2);
            
            //---------
            
            var spellsDictionary = spellsIn.allSpells.Select(x => new Entry
            {
                Key = x.en.name.Trim().ToLower(),
                Type = Type.Spell,
                En = new Lang
                {
                    Name = x.en.name,
                    Description = x.en.text
                },
                Ru = new Lang
                {
                    Name = x.ru.name,
                    Description = x.ru.text
                }
            }).ToList();

            var itemsDictionary = itemsIn.itemsList.Select(x => new Entry
            {
                Key = x.en.name.Trim().ToLower(),
                Type = Type.Item,
                En = new Lang
                {
                    Name = x.en.name,
                    Description = x.en.text
                },
                Ru = new Lang
                {
                    Name = x.ru.name,
                    Description = x.ru.text
                }
            }).ToList();
            
            itemsDictionary.AddRange(items2In.itemsList.Select(x => new Entry
            {
                Key = x.en.name.Trim().ToLower(),
                Type = Type.Item,
                En = new Lang
                {
                    Name = x.en.name,
                    Description = x.en.text
                },
                Ru = new Lang
                {
                    Name = x.ru.name,
                    Description = x.ru.text
                }
            }));

            var monstersDictionary = new List<Entry>();
            var monsterKeyRegex = new Regex(@"\(([a-zA-Z0-9+\-_ ]+)\)");

            foreach (var monsterData in monstersIn.dataList)
            {
                var match = monsterKeyRegex.Match(monsterData.name);
                
                if (match.Groups.Count < 2)
                    continue;

                var key = match.Groups[1].Value.Trim().ToLower();
                var name = monsterData.name;
                var description = string.Empty;

                #region Trait
                if (monsterData.trait.ValueKind == JsonValueKind.Array)
                {
                    var rawText = monsterData.trait.GetRawText();
                    var obj = JsonSerializer.Deserialize<List<@in.Trait>>(rawText);
                    
                    foreach (var trait in obj)
                    {
                        var traitName = trait.name;

                        description += $"<p><b>{traitName}:</b></br>";

                        if (trait.text.ValueKind == JsonValueKind.Array)
                        {
                            var lines = JsonSerializer.Deserialize<List<String>>(trait.text.GetRawText());
                            
                            lines.ForEach(x => description += $"{x}</br>");
                        }
                        else if (trait.text.ValueKind == JsonValueKind.String)
                        {
                            description += $"{trait.text.GetString()}";
                        }

                        description += "</p>";
                    }
                } 
                else if (monsterData.trait.ValueKind == JsonValueKind.Object)
                {
                    var rawText = monsterData.trait.GetRawText();
                    var obj = JsonSerializer.Deserialize<@in.Trait>(rawText);

                    var traitName = obj.name;
                    
                    description += $"<p><b>{traitName}:</b></br>";

                    if (obj.text.ValueKind == JsonValueKind.Array)
                    {
                        var lines = JsonSerializer.Deserialize<List<String>>(obj.text.GetRawText());
                        
                        lines.ForEach(x => description += $"{x}</br>");
                    }
                    else if (obj.text.ValueKind == JsonValueKind.String)
                    {
                        description += $"{obj.text.GetString()}";
                    }

                    description += "</p>";
                }
                #endregion
                
                #region Action

                if (monsterData.action.ValueKind == JsonValueKind.Array)
                {
                    var obj = JsonSerializer.Deserialize<List<@in.Action>>(monsterData.action.GetRawText());

                    foreach (var action in obj)
                    {
                        description += $"<p><b>{action.name}:</b></br>";

                        if (action.attack.ValueKind == JsonValueKind.Array)
                        {
                            var lines = JsonSerializer.Deserialize<List<string>>(action.attack.GetRawText());
                            
                            lines.ForEach(x => description += $"<i>{x}</i></br>");
                        }
                        else if (action.attack.ValueKind == JsonValueKind.String)
                        {
                            description += $"<i>{action.attack.GetString()}</i></br>";
                        }

                        if (action.text.ValueKind == JsonValueKind.Array)
                        {
                            var lines = JsonSerializer.Deserialize<List<String>>(action.text.GetRawText());
                            
                            lines.ForEach(x => description += $"{x}</br>");
                        }
                        else if (action.text.ValueKind == JsonValueKind.String)
                        {
                            description += $"{action.text.GetString()}";
                        }

                        description += "</p>";
                    }
                }
                else if (monsterData.action.ValueKind == JsonValueKind.Object)
                {
                    var action = JsonSerializer.Deserialize<@in.Action>(monsterData.action.GetRawText());

                    description += $"<p><b>{action.name}</b></br>:";
                    
                    if (action.attack.ValueKind == JsonValueKind.Array)
                    {
                        var lines = JsonSerializer.Deserialize<List<string>>(action.attack.GetRawText());
                            
                        lines.ForEach(x => description += $"<i>{x}</i></br>");
                    }
                    else if (action.attack.ValueKind == JsonValueKind.String)
                    {
                        description += $"<i>{action.attack.GetString()}</i></br>";
                    }
                    
                    if (action.text.ValueKind == JsonValueKind.Array)
                    {
                        var lines = JsonSerializer.Deserialize<List<String>>(action.text.GetRawText());
                            
                        lines.ForEach(x => description += $"{x}</br>");
                    }
                    else if (action.text.ValueKind == JsonValueKind.String)
                    {
                        description += $"{action.text.GetString()}";
                    }

                    description += "</p>";
                }
                #endregion

                monstersDictionary.Add(new Entry
                {
                    Key = key,
                    Type = Type.Monster,
                    En = new Lang
                    {
                        Name = key
                    },
                    Ru = new Lang
                    {
                        Name = name,
                        Description = description
                    }
                });
            }
            
            //---------

            spellsOut.label = "Заклинания (SRD)";
            foreach (var spellsOutEntry in spellsOut.entries)
            {
                var id = spellsOutEntry.id.Trim().ToLower();
                var c = FindInDictionary(spellsDictionary, id);

                if (c == null)
                    continue;

                var name = c.Ru.Name;
                var desciption = c.Ru.Description;
                
                spellsOutEntry.name = Regex.Replace(spellsOutEntry.name,
                    $"\\b{c.Key}\\b",
                    name,
                    RegexOptions.IgnoreCase);

                spellsOutEntry.description = !string.IsNullOrEmpty(desciption) ? $"<p>{desciption}</p>" : spellsOutEntry.description;
                spellsOutEntry.localized = true;
            }
            LocalizeEntriesThrowYandex(spellsOut.entries);

            itemsOut.label = "Предметы (SRD)";
            foreach (var itemsOutEntry in itemsOut.entries)
            {
                var id = itemsOutEntry.id.Trim().ToLower();
                var c = FindInDictionary(itemsDictionary, id);

                if (c == null)
                    continue;

                var name = c.Ru.Name;
                var description = c.Ru.Description;

                itemsOutEntry.name = Regex.Replace(itemsOutEntry.name, 
                    $"\\b{c.Key}\\b", 
                    name, 
                    RegexOptions.IgnoreCase);

                if (!string.IsNullOrEmpty(description))
                    itemsOutEntry.description = description;
                
                itemsOutEntry.localized = true;
            }
            LocalizeEntriesThrowYandex(itemsOut.entries);

            monsterOut.label = "Бестиарий (SRD)";
            foreach (var monsterOutEntry in monsterOut.entries)
            {
                var id = monsterOutEntry.id.Trim().ToLower();
                var c = FindInDictionary(monstersDictionary, id);

                if (c == null)
                    continue;

                var name = c.Ru.Name;
                var description = c.Ru.Description;

                monsterOutEntry.name = Regex.Replace(monsterOutEntry.name,
                    $"\\b{c.Key}\\b",
                    name,
                    RegexOptions.IgnoreCase);

                if (!string.IsNullOrEmpty(description))
                    monsterOutEntry.description = description;
                
                monsterOutEntry.localized = true;
            }
            LocalizeEntriesThrowYandex(monsterOut.entries);
            
            //---------

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var lf0 = JsonSerializer.SerializeToUtf8Bytes(spellsOut, serializerOptions);
            File.WriteAllBytes($"{outPath}/dnd5e.spells.json", lf0);

            var lf1 = JsonSerializer.SerializeToUtf8Bytes(itemsOut, serializerOptions);
            File.WriteAllBytes($"{outPath}/dnd5e.items.json", lf1);

            var lf2 = JsonSerializer.SerializeToUtf8Bytes(monsterOut, serializerOptions);
            File.WriteAllBytes($"{outPath}/dnd5e.monsters.json", lf2);
        }
    }
}