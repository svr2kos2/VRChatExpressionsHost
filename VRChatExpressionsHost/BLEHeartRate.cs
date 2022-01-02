using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using System.Threading;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using System.Net;
using System.Net.Sockets;

namespace VRChatExpressionsHost
{
    class BLEHeartRate
    {
        private static T AsyncResult<T>(IAsyncOperation<T> async)
        {
            while (true)
            {
                switch (async.Status)
                {
                    case AsyncStatus.Started:
                        Thread.Sleep(10);
                        continue;
                    case AsyncStatus.Completed:
                        return async.GetResults();
                    case AsyncStatus.Error:
                        throw async.ErrorCode;
                    case AsyncStatus.Canceled:
                        throw new TaskCanceledException();
                }
            }
        }

        public enum Status 
        { 
            STOPED, 
            STARTING, 
            ERROR, 
            RUNNING 
        };
        enum ContactSensorStatus
        {
            NotSupported,
            NotSupported2,
            NoContact,
            Contact
        }

        Thread BLEThread;

        DeviceInformation device;
        GattDeviceService service;
        GattCharacteristic heartrate;
        
        public Status CurrentStatus;
        public DateTime lastupdate;

        public delegate void ValueChangedHandler(int heartRate);
        public event ValueChangedHandler ValueChangedEvent;

        public BLEHeartRate()
        {
            CurrentStatus = Status.STOPED;
            lastupdate = DateTime.Now;
            BLEThread = new Thread(this.ProcessThread);
            BLEThread.Start();
        }

        ~BLEHeartRate()
        {
            BLEThread.Abort();
        }

        void ProcessThread()
        {
            for (; ; )
            {
                if (CurrentStatus == Status.STOPED || CurrentStatus == Status.ERROR || (CurrentStatus == Status.RUNNING && (DateTime.Now - lastupdate).TotalSeconds > 5))
                {
                    Console.WriteLine("Retry");
                    Initialize();
                    Thread.Sleep(3000);
                }
            }
        }

        void Initialize()
        {
            CurrentStatus = Status.STARTING;
            try
            {
                var heartrateSelector = GattDeviceService
                    .GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate);
                if(heartrateSelector == null)
                {
                    CurrentStatus = Status.ERROR; return;
                }
                var devices = AsyncResult(DeviceInformation
                    .FindAllAsync(heartrateSelector));
                if (devices == null)
                {
                    CurrentStatus = Status.ERROR; return;
                }
                device = devices.FirstOrDefault();
                service = AsyncResult(GattDeviceService.FromIdAsync(device.Id));
                const int _heartRateMeasurementCharacteristicId = 0x2A37;

                if (service == null)
                {
                    CurrentStatus = Status.ERROR; return;
                }

                heartrate = AsyncResult(service.GetCharacteristicsForUuidAsync(BluetoothUuidHelper.FromShortId(
                    _heartRateMeasurementCharacteristicId))).Characteristics.FirstOrDefault();

                var status = AsyncResult(
                    heartrate.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify));

                heartrate.ValueChanged += HeartRate_ValueChanged;
                CurrentStatus = Status.RUNNING;
                lastupdate = DateTime.Now;
            }
            catch
            {
                Console.WriteLine("Failed");
                CurrentStatus = Status.ERROR;
            }
        }

        void HeartRate_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            lastupdate = DateTime.Now;
            try
            {
                var value = args.CharacteristicValue;
                if (value.Length == 0)
                {
                    return;
                }

                using (var reader = DataReader.FromBuffer(value))
                {
                    var bpm = -1;
                    var flags = reader.ReadByte();
                    var isshort = (flags & 1) == 1;
                    var contactSensor = (ContactSensorStatus)((flags >> 1) & 3);
                    var minLength = isshort ? 3 : 2;

                    if (value.Length < minLength)
                    {
                        //Console.WriteLine($"Buffer was too small. Got {value.Length}, expected {minLength}.");
                        return;
                    }

                    if (value.Length > 1)
                    {
                        bpm = isshort
                            ? reader.ReadUInt16()
                            : reader.ReadByte();
                    }
                    
                    if (ValueChangedEvent != null)
                        ValueChangedEvent(bpm);
                }
            }
            catch
            {
                //Console.WriteLine("BLEHR read faild");
            }
        }

    }
}
