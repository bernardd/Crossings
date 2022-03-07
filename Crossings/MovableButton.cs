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

        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                m_deltaPos = absolutePosition - mousePos;
                BringToFront();
            }
        }
        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                absolutePosition = mousePos + m_deltaPos;
                savedX.value = (int)absolutePosition.x;
                savedY.value = (int)absolutePosition.y;
            }
        }
    }
}
