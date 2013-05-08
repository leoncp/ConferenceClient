using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConferenceClient
{
    public class Asset
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string MetaD { get; set; }
        public int Duration { get; set; }
    }
}
