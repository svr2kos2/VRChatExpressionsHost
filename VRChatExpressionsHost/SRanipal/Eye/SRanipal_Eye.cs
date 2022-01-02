//========= Copyright 2019, HTC Corporation. All rights reserved. ===========
using System;
using System.Collections.Generic;

namespace ViveSR
{
    namespace anipal
    {
        namespace Eye
        {
            public static class SRanipal_Eye
            {
                public const int ANIPAL_TYPE_EYE = 0;
                public delegate void CallbackBasic(ref EyeData data);

                /// <summary>
                /// Register a callback function to receive eye camera related data when the module has new outputs.
                /// </summary>
                /// <param name="callback">function pointer of callback</param>
                /// <returns>error code. please refer Error in ViveSR_Enums.h.</returns>
                public static int WrapperRegisterEyeDataCallback(System.IntPtr callback)
                {
                    return SRanipal_Eye_API.RegisterEyeDataCallback(callback);
                }

                /// <summary>
                /// Unregister a callback function to receive eye camera related data when the module has new outputs.
                /// </summary>
                /// <param name="callback">function pointer of callback</param>
                /// <returns>error code. please refer Error in ViveSR_Enums.h.</returns>
                public static int WrapperUnRegisterEyeDataCallback(System.IntPtr callback)
                {
                    return SRanipal_Eye_API.UnregisterEyeDataCallback(callback);
                }

                public const int WeightingCount = (int)EyeShape.Max;
                private static EyeData EyeData_ = new EyeData();
                private static Error LastUpdateResult = Error.FAILED;
                private static Dictionary<EyeShape, float> Weightings;

                static SRanipal_Eye()
                {
                    Weightings = new Dictionary<EyeShape, float>();
                    for (int i = 0; i < WeightingCount; ++i) Weightings.Add((EyeShape)i, 0.0f);
                }
                private static bool UpdateData()
                {
                    LastUpdateResult = SRanipal_Eye_API.GetEyeData(ref EyeData_);
                    return LastUpdateResult == Error.WORK;
                }

                /// <summary>
                /// Gets the VerboseData of anipal's Eye module when enable eye callback function.
                /// </summary>
                /// <param name="data">ViveSR.anipal.Eye.VerboseData</param>
                /// <param name="data">ViveSR.anipal.Eye.EyeData</param>
                /// <returns>Indicates whether the data received is new.</returns>
                public static bool GetVerboseData(out VerboseData data, EyeData eye_data)
                {
                    data = eye_data.verbose_data;
                    return true;
                }

                /// <summary>
                /// Gets the VerboseData of anipal's Eye module.
                /// </summary>
                /// <param name="data">ViveSR.anipal.Eye.VerboseData</param>
                /// <returns>Indicates whether the data received is new.</returns>
                public static bool GetVerboseData(out VerboseData data)
                {
                    UpdateData();
                    return GetVerboseData(out data, EyeData_);
                }

                /// <summary>
                /// Gets the openness value of an eye when enable eye callback function.
                /// </summary>
                /// <param name="eye">The index of an eye.</param>
                /// <param name="openness">The openness value of an eye, clamped between 0 (fully closed) and 1 (fully open). </param>
                /// <param name="eye_data">ViveSR.anipal.Eye.EyeData. </param>
                /// <returns>Indicates whether the openness value received is valid.</returns>
                public static bool GetEyeOpenness(EyeIndex eye, out float openness, EyeData eye_data)
                {
                    bool valid = true;
                    if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
                    {
                        SingleEyeData eyeData = eye == EyeIndex.LEFT ? eye_data.verbose_data.left : eye_data.verbose_data.right;
                        valid = eyeData.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_EYE_OPENNESS_VALIDITY);
                        openness = valid ? eyeData.eye_openness : 0;
                    }
                    else
                    {
                        // If not support eye tracking, set default to open.
                        openness = 1;
                    }
                    return valid;
                }

                /// <summary>
                /// Gets the openness value of an eye.
                /// </summary>
                /// <param name="eye">The index of an eye.</param>
                /// <param name="openness">The openness value of an eye, clamped between 0 (fully closed) and 1 (fully open). </param>
                /// <returns>Indicates whether the openness value received is valid.</returns>
                public static bool GetEyeOpenness(EyeIndex eye, out float openness)
                {
                    UpdateData();
                    return GetEyeOpenness(eye, out openness, EyeData_);
                }


                /// <summary>
                /// Tests eye gaze data when enable eye callback function.
                /// </summary>
                /// <param name="validity">A type of eye gaze data to test with.</param>
                /// <param name="gazeIndex">The index of a source of eye gaze data.</param>
                /// <param name="eye_data">ViveSR.anipal.Eye.EyeData. </param>
                /// <returns>Indicates whether a source of eye gaze data is found.</returns>
                public static bool TryGaze(SingleEyeDataValidity validity, out GazeIndex gazeIndex, EyeData eye_data)
                {
                    bool[] valid = new bool[(int)GazeIndex.COMBINE + 1] { eye_data.verbose_data.left.GetValidity(validity),
                                                                          eye_data.verbose_data.right.GetValidity(validity),
                                                                          eye_data.verbose_data.combined.eye_data.GetValidity(validity)};
                    gazeIndex = GazeIndex.COMBINE;
                    for (int i = (int)GazeIndex.COMBINE; i >= 0; --i)
                    {
                        if (valid[i])
                        {
                            gazeIndex = (GazeIndex)i;
                            return true;
                        }
                    }
                    return false;
                }


                /// <summary>
                /// Tests eye gaze data.
                /// </summary>
                /// <param name="validity">A type of eye gaze data to test with.</param>
                /// <param name="gazeIndex">The index of a source of eye gaze data.</param>
                /// <returns>Indicates whether a source of eye gaze data is found.</returns>
                public static bool TryGaze(SingleEyeDataValidity validity, out GazeIndex gazeIndex)
                {
                    UpdateData();
                    return TryGaze(validity, out gazeIndex, EyeData_);
                }

                
                /// <summary>
                /// Launches anipal's Eye Calibration feature (an overlay program).
                /// </summary>
                /// <returns>Indicates the resulting ViveSR.Error status of this method.</returns>
                public static bool LaunchEyeCalibration()
                {
                    int result = SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
                    return result == (int)Error.WORK;
                }
            }
        }
    }
}