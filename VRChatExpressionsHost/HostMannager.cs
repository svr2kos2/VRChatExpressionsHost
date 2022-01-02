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
        static UdpClient udpcSend = null;
        static IPEndPoint localIpep = null;
        static IPEndPoint remotelpep = null;
        static DateTime lastSRanipaCallback;
        public static void Start()
        {
            lastSRanipaCallback = DateTime.Now;

            localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 15737);
            remotelpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 15739);
            udpcSend = new UdpClient(localIpep);

            //BLEHeartRate heartRate = new BLEHeartRate();
            //heartRate.ValueChangedEvent += ValueChanged;
            eye_Framework = new SRanipal_Eye_Framework();
            lip_Framework = new SRanipal_Lip_Framework();
            eye_Framework.EnableEyeVersion = SRanipal_Eye_Framework.SupportedEyeVersion.version2;
            lip_Framework.EnableLipVersion = SRanipal_Lip_Framework.SupportedLipVersion.version2;
            eye_Framework.StartFramework();
            lip_Framework.StartFramework();
            SRanipal_Eye_API.RegisterEyeDataCallback_v2(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)SRanipalCallback));
            
        }

        public static void Close()
        {
            eye_Framework.StopFramework();
            lip_Framework.StopFramework();
        }

        public static void SRanipalCallback(ref EyeData_v2 eye_data_ref)
        {
            Console.WriteLine("cb");
            return;
            if ((DateTime.Now - lastSRanipaCallback).TotalMilliseconds < 10)
                return;
            lastSRanipaCallback = DateTime.Now;
            EyeData_v2 eye_data = eye_data_ref;
            LipData_v2 lip_data = new LipData_v2();
            SRanipal_Lip_API.GetLipData_v2(ref lip_data);
            Vector3 leftDir = eye_data.verbose_data.left.gaze_direction_normalized;
            Vector3 rightDir = eye_data.verbose_data.right.gaze_direction_normalized;
            Vector3 comDir = eye_data.verbose_data.combined.eye_data.gaze_direction_normalized;
            const float Rad2Deg = 57.29578f;

            float EyeX = -(float) Math.Asin(comDir.x / Math.Sqrt(1 - Math.Pow(comDir.y, 2))) * Rad2Deg;
            float EyeY = -(float)Math.Asin(comDir.y / Math.Sqrt(1 - Math.Pow(comDir.x, 2))) * Rad2Deg;
            float LeftEyeOpeness = eye_data.verbose_data.left.eye_openness;
            float RightEyeOpeness = eye_data.verbose_data.right.eye_openness;
            float SmileSad = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Smile_Left)] +
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Smile_Right)] -
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Sad_Left)] -
                lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Mouth_Sad_Right)];

            float MouthA = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Jaw_Open)];
            float Tong = lip_data.prediction_data.blend_shape_weight[((int)LipShape_v2.Tongue_LongStep2)];

            EyeX /= 20.0f;
            EyeY /= 20.0f;
            SmileSad /= 2.0f;

            LeftEyeOpeness = (LeftEyeOpeness > 0.5 && leftDir.x == 0 && leftDir.y == 0) ? 0 : 1;
            RightEyeOpeness = (RightEyeOpeness > 0.5 && rightDir.x == 0 && rightDir.y == 0) ? 0 : 1;

            

            string msg = "EyeX " + EyeX +
                ";EyeY " + EyeY +
                ";LeftEyeOpeness " + LeftEyeOpeness +
                ";RightEyeOpeness " + RightEyeOpeness +
                ";SmileSad " + SmileSad +
                ";MouthA " + MouthA +
                "Tong " + Tong;
            SendData(msg);
        }

        public static void ValueChanged(int bmp)
        {
            Console.WriteLine(bmp);
            SendData("HeartRate " + (bmp+1)/200.0);
        }

        public static void SendData(string data)
        {
            if (udpcSend == null || remotelpep == null)
                return;
            byte[] sendbytes = Encoding.UTF8.GetBytes(data);
            udpcSend.Send(sendbytes, sendbytes.Length, remotelpep);
        }

    }
}
