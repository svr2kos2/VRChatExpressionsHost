using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRChatExpressionsHost
{
    class FacialTracking
    {
        public enum FrameworkStatus { STOP, START, WORKING, ERROR, NOT_SUPPORT }
        public static FrameworkStatus Status { get; protected set; }

        void StartFramework()
        {
            if (Status == FrameworkStatus.WORKING || Status == FrameworkStatus.NOT_SUPPORT) return;

        }
    }
}
