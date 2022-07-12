using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViveSR.anipal.Eye;
using ViveSR.anipal.Lip;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Microsoft.Win32;
using System.Diagnostics;

namespace VRChatExpressionsHost
{

    public class AppMain
    {
        public static void Main(string[] args)
        {

            HostMannager.Start();
            for (; ; )Thread.Sleep(1000);
        }
    }

    public class HostMannager
    {
        static SRanipal_Eye_Framework eye_Framework;
        static SRanipal_Lip_Framework lip_Framework;
        static BLEHeartRate heartRate;
        static UdpClient udpcSend = null;
        static IPEndPoint localIpep = null;
        static IPEndPoint remotelpep = null;
        static DateTime lastSRanipaCallback;
        static int HeartRate = 0;
        static System.Timers.Timer UpdateTimer;
        static Queue<float>leftEyeOpenessSmoth = new Queue<float>();
        static Queue<float> rightEyeOpenessSmoth = new Queue<float>();

        static DateTime timeCounter;

        public static void Start()
        {
            lastSRanipaCallback = DateTime.Now;

            localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9001);
            remotelpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            udpcSend = new UdpClient(localIpep);

            heartRate = new BLEHeartRate();
            heartRate.ValueChangedEvent += HeartRateChanged;

            DependencyProc(true);

            StartSRanipalFramework();

            leftEyeOpenessSmoth.Enqueue(1.0f);
            rightEyeOpenessSmoth.Enqueue(1.0f);

            timeCounter = DateTime.Now;

            UpdateTimer = new System.Timers.Timer();
            UpdateTimer.Interval =1;
            UpdateTimer.AutoReset = true;
            UpdateTimer.Elapsed += Update;
            UpdateTimer.Start();
            //SRanipal_Eye_API.RegisterEyeDataCallback_v2(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)SRanipalCallback));
            
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FreeLibrary(string lpFileName);
        static public void DependencyProc(bool isLoad)
        {
            try
            {
                string key = @"SYSTEM\CurrentControlSet\Services\SRanipalService";
                string path = Registry.LocalMachine.OpenSubKey(key).GetValue("ImagePath").ToString();
                path = path.Replace("\"", string.Empty).Substring(0, path.LastIndexOf('\\'));
                string[] DllToLoad = {
                    @"tools\eye_calibration\EyeCalibration_Data\Plugins\libHTC_License.dll",
                    @"nanomsg.dll",
                    @"SRWorks_Log.dll",
                    @"tools\eye_calibration\EyeCalibration_Data\Plugins\ViveSR_Client.dll",
                    @"tools\eye_calibration\EyeCalibration_Data\Plugins\SRanipal.dll"};
                foreach (var dll in DllToLoad)
                {
                    if (isLoad)
                    {
                        LoadLibrary(path + dll);
                    }
                    else
                    {
                        FreeLibrary(path + dll);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Can't find SRanipal servece. You may not install SRanipal runtime yet.");
            }
        }

        static void StartSRanipalFramework()
        {
            eye_Framework = new SRanipal_Eye_Framework();
            lip_Framework = new SRanipal_Lip_Framework();
            eye_Framework.EnableEyeVersion = SRanipal_Eye_Framework.SupportedEyeVersion.version2;
            lip_Framework.EnableLipVersion = SRanipal_Lip_Framework.SupportedLipVersion.version2;
            eye_Framework.StartFramework();
            lip_Framework.StartFramework();
        }

        public static void Close()
        {
            UpdateTimer.Stop();
            eye_Framework.StopFramework();
            lip_Framework.StopFramework();
        }

        static float OpenessSmooth(ref Queue<float> q,float target)
        {
            if (q.Count == 1)
            {
                float del = target - q.Peek();
                del = del / Math.Abs(del);
                del /= 20.0f;
                for (float openess = q.Dequeue(); Math.Abs(target - openess) > 0.05f; openess += del)
                    q.Enqueue(openess);
            }
            return leftEyeOpenessSmoth.Dequeue();
        }

        static float currentLeftEyeOpeness = 0;
        static float currentRightEyeOpeness = 0;


        static void SRanipalDataTrans(EyeData_v2 eye_data, LipData_v2 lip_data)
        {
            Vector3 leftDir = eye_data.verbose_data.left.gaze_direction_normalized;
            Vector3 rightDir = eye_data.verbose_data.right.gaze_direction_normalized;
            Vector3 comDir = eye_data.verbose_data.combined.eye_data.gaze_direction_normalized;
            const float Rad2Deg = 57.29578f;


            //float EyeX = comDir.x;
            //float EyeY = comDir.y;

            float EyeX = -(float)Math.Asin(comDir.x / Math.Sqrt(1 - Math.Pow(comDir.y, 2))) * Rad2Deg;
            float EyeY = (float)Math.Asin(comDir.y / Math.Sqrt(1 - Math.Pow(comDir.x, 2))) * Rad2Deg;
            float LeftEyeOpeness = eye_data.verbose_data.left.eye_openness;
            float RightEyeOpeness = eye_data.verbose_data.right.eye_openness;
            float SmileSad = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Smile_Left)] +
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Smile_Right)] -
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Sad_Left)] -
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Sad_Right)];

            float MouthA = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Jaw_Open)];
            float MouthO = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Pout)];
            float Tong = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Tongue_LongStep2)];

            EyeX /= 20.0f;
            EyeY /= 20.0f;
            SmileSad /= 2.0f;

            MouthA *= 2.0f;
            MouthO *= 2.0f;

            SmileSad *= 1.5f;

            //LeftEyeOpeness = (LeftEyeOpeness < 0.5 && leftDir.x == 0 && leftDir.y == 0) ? 0 : 1;
            //RightEyeOpeness = (RightEyeOpeness < 0.5 && rightDir.x == 0 && rightDir.y == 0) ? 0 : 1;

            //LeftEyeOpeness = OpenessSmooth(ref leftEyeOpenessSmoth,LeftEyeOpeness);
            //RightEyeOpeness = OpenessSmooth(ref rightEyeOpenessSmoth, RightEyeOpeness);


            SendData("/avatar/parameters/EyeX", EyeX);
            SendData("/avatar/parameters/EyeY", EyeY);
            SendData("/avatar/parameters/LeftEyeOpeness",currentLeftEyeOpeness = Mathf.MoveTowards(currentLeftEyeOpeness,LeftEyeOpeness,0.1f));
            SendData("/avatar/parameters/RightEyeOpeness",currentRightEyeOpeness = Mathf.MoveTowards(currentRightEyeOpeness, RightEyeOpeness, 0.1f));
            SendData("/avatar/parameters/SmileSad", SmileSad);
            SendData("/avatar/parameters/MouthA", MouthA);
            SendData("/avatar/parameters/MouthO", MouthO);
            SendData("/avatar/parameters/Tong", Tong);
            //string msg = "EyeX " + EyeX +
            //    ";EyeY " + EyeY +
            //    ";LeftEyeOpeness " + LeftEyeOpeness +
            //    ";RightEyeOpeness " + RightEyeOpeness +
            //    ";SmileSad " + SmileSad +
            //    ";MouthA " + MouthA +
            //    ";MouthO " + MouthO +
            //    ";Tong " + Tong;
            //return msg;
        }

        static bool pulling = false;
        static int count = 0;
        static long tryCount = 0;
        public static void Update(object sender, System.Timers.ElapsedEventArgs e)
        {
            
            ++tryCount;
            if (Process.GetProcessesByName("sr_runtime").Length == 0)
            {
                UpdateTimer.Stop();
                Console.WriteLine("SRanipal Crashed");
                Thread.Sleep(500);
                DependencyProc(false);
                Thread.Sleep(100);
                DependencyProc(true);
                string key = @"SYSTEM\CurrentControlSet\Services\SRanipalService";
                string path = Registry.LocalMachine.OpenSubKey(key).GetValue("ImagePath").ToString();
                path = path.Replace("\"", string.Empty).Substring(0, path.LastIndexOf('\\'));
                Console.WriteLine("Start " + path + "sr_runtime.exe");
                Process.Start(path + "sr_runtime.exe");
                Thread.Sleep(4000);
                StartSRanipalFramework();
                UpdateTimer.Start();
            } else if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING && SRanipal_Lip_Framework.Status == SRanipal_Lip_Framework.FrameworkStatus.WORKING)
            {
                if(pulling)
                {
                    //Console.WriteLine("pulling ");
                    return;
                }
                EyeData_v2 eye_data = new EyeData_v2();
                LipData_v2 lip_data = new LipData_v2();
                try
                {
                    pulling = true;
                    SRanipal_Eye_API.GetEyeData_v2(ref eye_data);
                    SRanipal_Lip_API.GetLipData_v2(ref lip_data);
                    pulling = false;
                    SRanipalDataTrans(eye_data, lip_data);
                    //res += SRanipalDataTrans(eye_data, lip_data);
                    ++count;
                    Console.WriteLine("refresh rate:" + count / (DateTime.Now - timeCounter).TotalSeconds + "  " + (double)tryCount / (DateTime.Now - timeCounter).TotalSeconds);
                } catch
                {
                    Console.WriteLine("GetData_v2 failed.");
                }
            }
            
        }

        public static void SRanipalCallback(ref EyeData_v2 eye_data_ref)
        {
            if ((DateTime.Now - lastSRanipaCallback).TotalMilliseconds < 10)
                return;
            lastSRanipaCallback = DateTime.Now;
            EyeData_v2 eye_data = eye_data_ref;
            LipData_v2 lip_data = new LipData_v2();
            SRanipal_Lip_API.GetLipData_v2(ref lip_data);
        }

        public static void HeartRateChanged(int bmp)
        {
            //Console.WriteLine(bmp);
            HeartRate = bmp;
            SendData("/avatar/parameters/HeartRate",(HeartRate + 1) / 200.0f);
            //SendData("HeartRate " + (bmp+1)/200.0);
        }

        public static void SendData(string path,float value)
        {
            List<byte> res = new List<byte>();
            foreach (byte b in path)
                res.Add(b);
            if (res.Count % 4 == 0)
                res.Add(0);
            for(;res.Count %4 != 0;)
                res.Add(0);
            res.AddRange(new byte[] { (byte)',', (byte)'f', 0x00, 0x00 });
            var bvalue = new List<byte>(BitConverter.GetBytes(value));
            bvalue.Reverse();
            res.AddRange(bvalue.ToArray());
            //Console.WriteLine("update " + data);
            if (udpcSend == null || remotelpep == null)
                return;
            byte[] sendbytes = res.ToArray();
            string str = "";
            foreach (byte b in sendbytes)
                str += b.ToString("X2");
            Console.WriteLine("t " + path + " " + value + " " + str);
            udpcSend.Send(sendbytes, sendbytes.Length, remotelpep);
        }

    }
}
