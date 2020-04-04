namespace dnd_lang.lang
{
    public enum Type
    {
        Spell, Item, Monster
    }
    
    public class Entry
    {
        public string Key { get; set; }
        public Type Type { get; set; }
        public Lang En { get; set; }
        public Lang Ru { get; set; }
    }
}