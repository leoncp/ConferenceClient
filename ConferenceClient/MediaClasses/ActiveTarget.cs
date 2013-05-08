using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConferenceClient
{
    /// <summary>
    /// The ActiveTarget class is used in a collection to hold that State of Screen
    /// Targets once they are built during Template Layout.  Once the collection is
    /// populated, the &quot;idle&quot; state of the targets is monitored to decide if
    /// they need to be populated and triggered with additional Media.
    /// </summary>
    class ActiveTarget
    {
        public int targetid {get; set; }
        public string type { get; set; }
        public string busywith { get; set; }
        public Int32 templateid { get; set; }
        public Int32 layer { get; set; }
        public string meta { get; set; }
        public bool active { get; set; }
        public bool idle { get; set; }
        public int secsremaining { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public double opacity { get; set; }
        public int activecontentserial { get; set; }
        public int currentShowOrder { get; set; }
    }
}
