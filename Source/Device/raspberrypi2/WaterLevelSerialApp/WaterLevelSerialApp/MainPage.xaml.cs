using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.System.Threading;
using System.Threading;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json;



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WaterLevelSerialApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //private string arduioSerialData;
        //private DeviceInformation[] entry = new DeviceInformation[3];
        static DeviceClient deviceClient;
        static string iotHubUri = "WaterLevelTest.azure-devices.net";
        static string deviceKey = "6H2wDPSBgPV97r9nQq5NtUkZIsWgqfYvUBBZNfWyKSs=";

        private int count = 0;
        private DispatcherTimer timer;
        private struct iotJsonDataParse
        {
            public string guid;
            public string organization;
            public string displayname;
            public string location;
            public string measurename;
            public string unitofmeasure;
            public string value;
        }
        private static iotJsonDataParse waterLevel;
        private static iotJsonDataParse motorStatus;
        public static SerialAdruino serialConnction = new SerialAdruino();
        private SolidColorBrush lightBlueBrush = new SolidColorBrush(Windows.UI.Colors.LightBlue);
        private SolidColorBrush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.Green);
        public MainPage()
        {
            this.InitializeComponent();
            //Task task = AutoConnectSerial();
            //CheckSerialConnection();
            InitilizeSerialConnaction();

            count = serialConnction.NumSerialConnection();
            Debug.WriteLine("END init serial");
            if (count <= 0)
            {
                Dbg.Text = "NO COM PORT AVILABLE";
            }
            else {
                connectSerialPort(0);
                if (serialConnction.GetSerialHandle() != null)
                    Dbg.Text = serialConnction.GetPort();
                // = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey("myFirstDevice", deviceKey), TransportType.Http1);
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Timer_Tick;
                timer.Start();
                //SendDeviceToCloudMessagesAsync();

            }


        }

        private async void InitilizeSerialConnaction()
        {

             serialConnction.InitSerial();
        }

        private async void connectSerialPort(int entry)
        {
            serialConnction.connectSerial(entry);
            await Task.Delay(500);
        }

        private void Timer_Tick(object sender, object e)
        {
            char[] delimiterChars = { '}' };
            if (serialConnction.GetSerialHandle() != null)
            {
                //Dbg.Text = serialConnction.GetPort();
                //Debug.WriteLine(serialPort.PortName);
                if (serialConnction.serialDataAvilable)
                {
                    //arduioSerialData = serialConnction.GetSerialData();
                    //Debug.WriteLine(arduioSerialData);
                    string waterLevelBuff = serialConnction.GetSerialData();
                    Debug.WriteLine(waterLevelBuff);

                    if (waterLevelBuff.Length >= 300)
                    {
                        //int index = test.IndexOf("}");
                        string[] waterLevelString = waterLevelBuff.Split(delimiterChars);
                        Debug.WriteLine(waterLevelString[0]);
                        Debug.WriteLine(waterLevelString[1]);
                        waterLevelString[0] = waterLevelString[0] + "}";
                        waterLevelString[1] = waterLevelString[1] + "}";
                        //motorStatusString = test[index + 1];
                        dynamic data = JObject.Parse(waterLevelString[0]);
                        waterLevel.guid = data.guid;
                        waterLevel.organization = data.organization;
                        waterLevel.displayname = data.displayname;
                        waterLevel.location = data.location;
                        waterLevel.measurename = data.measurename;
                        waterLevel.unitofmeasure = data.unitofmeasure;
                        waterLevel.value = data.value;
                        dynamic data1 = JObject.Parse(waterLevelString[1]);
                        motorStatus.guid = data1.guid;
                        motorStatus.organization = data1.organization;
                        motorStatus.displayname = data1.displayname;
                        motorStatus.location = data1.location;
                        motorStatus.measurename = data1.measurename;
                        motorStatus.unitofmeasure = data1.unitofmeasure;
                        motorStatus.value = data1.value;
                        Debug.WriteLine("unitofmeasure", waterLevel.unitofmeasure);
                        Debug.WriteLine("value", waterLevel.value);
                        Debug.WriteLine("unitofmeasure", motorStatus.unitofmeasure);
                        //Debug.WriteLine("value", motorStatus.value);
                        //test.CopyTo(0,waterLevelString, 0, index);

                        serialConnction.serialDataAvilable = false;
                        if ((waterLevel.guid.Equals("WL2016-0000-0001-0001-000000002")) && (waterLevel.measurename.Equals("WaterLevel")))
                        {
                            switch (Int32.Parse(waterLevel.value))
                            {
                                case 3:
                                    WaterLeveHigh.Fill = lightBlueBrush;
                                    WaterLevelMedium.Fill = lightBlueBrush;
                                    WaterLevelLow.Fill = lightBlueBrush;
                                    break;

                                case 2:
                                    WaterLeveHigh.Fill = whiteBrush;
                                    WaterLevelMedium.Fill = lightBlueBrush;
                                    WaterLevelLow.Fill = lightBlueBrush;

                                    break;

                                case 1:
                                    WaterLeveHigh.Fill = whiteBrush;
                                    WaterLevelMedium.Fill = whiteBrush;
                                    WaterLevelLow.Fill = lightBlueBrush;
                                    break;

                                case 0:

                                    WaterLeveHigh.Fill = whiteBrush;
                                    WaterLevelMedium.Fill = whiteBrush;
                                    WaterLevelLow.Fill = whiteBrush;
                                    Dbg.Text = "water level empty";
                                    break;

                                case -1:
                                    Dbg.Text = "ERROR CONNECTION ";
                                    break;
                            }

                            //Console.ReadLine();
                        }
                        if ((motorStatus.guid.Equals("WL2016-0000-0001-0002-000000002")) && (motorStatus.measurename.Equals("MotorStatus")))
                        {
                            if (Int32.Parse(motorStatus.value) == 0)
                            {
                                Motor_Status.Text = "Motor Off";
                                MotorOff.Fill = redBrush;
                                MotorOn.Fill = whiteBrush;
                            }
                            else
                            {
                                Motor_Status.Text = "Motor On";
                                MotorOn.Fill = greenBrush;
                                MotorOff.Fill = whiteBrush;

                            }

                        }
                        SendDeviceToCloudMessagesAsync();
                    }

                }
            }
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            string deviceId = "WaterLevel";
            try {
                string msg = "{deviceId:"
                + waterLevel.guid
                + ",measurename1:"
                + waterLevel.measurename
                + ",Level:"
                + waterLevel.value
                + ",measurename2"
                +motorStatus.measurename
                + ",MotorStatus:"
                + motorStatus.value +"}";

                var deviceClient = DeviceClient.Create(iotHubUri,
                        AuthenticationMethodFactory.
                            CreateAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey),
                        TransportType.Http1);

                //var str = "Hello, Cloud!";

                var message = new Message(Encoding.ASCII.GetBytes(msg));

                await deviceClient.SendEventAsync(message);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


    }
}
