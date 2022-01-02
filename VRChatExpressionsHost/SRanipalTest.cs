using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ViveSR
{
    namespace anipal
    {

        static public class SRanipalTest
        {
            static Eye.SRanipal_Eye_Framework eye_Framework = new Eye.SRanipal_Eye_Framework();
            static Lip.SRanipal_Lip_Framework lip_Framework = new Lip.SRanipal_Lip_Framework();
            static public void Start()
            {
                eye_Framework.EnableEyeVersion = Eye.SRanipal_Eye_Framework.SupportedEyeVersion.version2;
                lip_Framework.EnableLipVersion = Lip.SRanipal_Lip_Framework.SupportedLipVersion.version2;
                eye_Framework.StartFramework();
                lip_Framework.StartFramework();
            }

            static public Eye.EyeData_v2 GetEyeData()
            {
                Eye.EyeData_v2 eye_data = new Eye.EyeData_v2();
                Eye.SRanipal_Eye_API.GetEyeData_v2(ref eye_data);
                return eye_data;
            }

            static public Lip.LipData_v2 GetLipData()
            {
                Lip.LipData_v2 lip_data = new Lip.LipData_v2();
                Lip.SRanipal_Lip_API.GetLipData_v2(ref lip_data);
                return lip_data;
            }

            static public void Stop()
            {
                eye_Framework.StopFramework();
                lip_Framework.StopFramework();
            }


        }
        
    }
}
