using System.Collections.Generic;

namespace SteamServerPicker.Models
{
    public class Route
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? Ranges { get; set; }
        public bool Pw { get; set; }
    }
}
