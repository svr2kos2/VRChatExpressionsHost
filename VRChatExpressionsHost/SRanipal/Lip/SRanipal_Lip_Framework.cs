//========= Copyright 2019, HTC Corporation. All rights reserved. ===========
using System;
using UnityEngine;

namespace ViveSR
{
    namespace anipal
    {
        namespace Lip
        {
            public class SRanipal_Lip_Framework 
            {
                public enum FrameworkStatus { STOP, START, WORKING, ERROR }
                /// <summary>
                /// The status of the anipal engine.
                /// </summary>
                public static FrameworkStatus Status { get; protected set; }
                /// <summary>
                /// Whether to enable anipal's Lip module.
                /// </summary>
                public bool EnableLip = true;

                /// <summary>
                /// Currently supported lip motion prediction engine's version.
                /// </summary>
                public enum SupportedLipVersion { version1, version2 }
                /// <summary>
                /// Which version of lip motion prediction engine will be used, default is version 1.
                /// </summary>
                public SupportedLipVersion EnableLipVersion = SupportedLipVersion.version1;

                void Start()
                {
                    StartFramework();
                }

                void OnDestroy()
                {
                    StopFramework();
                }

                public void StartFramework()
                {
                    if (!EnableLip) return;
                    if (Status == FrameworkStatus.WORKING) return;
                    Status = FrameworkStatus.START;

                    if (EnableLipVersion == SupportedLipVersion.version1)
                    {
                        Error result = SRanipal_API.Initial(SRanipal_Lip.ANIPAL_TYPE_LIP, IntPtr.Zero);
                        if (result == Error.WORK)
                        {
                            Console.WriteLine("[SRanipal] Initial Lip : " + result);
                            Status = FrameworkStatus.WORKING;
                        }
                        else
                        {
                            Console.WriteLine("[SRanipal] Initial Lip : " + result);
                            Status = FrameworkStatus.ERROR;
                        }
                    }
                    else
                    {
                        Error result = SRanipal_API.Initial(SRanipal_Lip_v2.ANIPAL_TYPE_LIP_V2, IntPtr.Zero);
                        if (result == Error.WORK)
                        {
                            Console.WriteLine("[SRanipal] Initial Version 2 Lip : " + result);
                            Status = FrameworkStatus.WORKING;
                        }
                        else
                        {
                            Console.WriteLine("[SRanipal] Initial Version 2 Lip : " + result);
                            Status = FrameworkStatus.ERROR;
                        }
                    }
                }

                public void StopFramework()
                {
                    if (Status != FrameworkStatus.STOP)
                    {
                        if (EnableLipVersion == SupportedLipVersion.version1)
                        {
                            Error result = SRanipal_API.Release(SRanipal_Lip.ANIPAL_TYPE_LIP);
                            if (result == Error.WORK) Console.WriteLine("[SRanipal] Release Lip : " + result);
                            else Console.WriteLine("[SRanipal] Release Lip : " + result);
                        }
                        else
                        {
                            Error result = SRanipal_API.Release(SRanipal_Lip_v2.ANIPAL_TYPE_LIP_V2);
                            if (result == Error.WORK) Console.WriteLine("[SRanipal] Release Version 2 Lip : " + result);
                            else Console.WriteLine("[SRanipal] Release Version 2 Lip : " + result);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[SRanipal] Stop Framework : module not on");
                    }
                    Status = FrameworkStatus.STOP;
                }
            }
        }
    }
}