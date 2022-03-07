using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crossings
{
    class MovableButton : UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", Crossings.settingsFileName, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", Crossings.settingsFileName, -1000, true);
    }
}
