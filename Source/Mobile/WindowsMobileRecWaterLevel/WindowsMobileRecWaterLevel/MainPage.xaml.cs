using System;
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

namespace WindowsMobileRecWaterLevel
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static DeviceClient deviceClient;
        //static DeviceClient deviceClient;
        static string iotHubUri = "WaterLevelTest.azure-devices.net";
        static string deviceKey = "6H2wDPSBgPV97r9nQq5NtUkZIsWgqfYvUBBZNfWyKSs=";
        static bool receiveDataStatus = false;
        static string messageData;
        //static EventHubClient eventHubClient;
        private struct iotJsonDataParse
        {
            public string guid;
            public string measurename1;
            public string unitofmeasure1;
            public string value1;
            public string measurename2;
            public string unitofmeasure2;
            public string value2;

        }
        private static iotJsonDataParse waterLevel;
        private DispatcherTimer timer;

        private SolidColorBrush lightBlueBrush = new SolidColorBrush(Windows.UI.Colors.LightBlue);
        private SolidColorBrush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.Green);

        public MainPage()
        {
            this.InitializeComponent();
            try {
                deviceClient = DeviceClient.Create(iotHubUri, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey("WaterLevel", deviceKey), TransportType.Http1);
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Timer_Tick;
                timer.Start();
                ReceiveCommands(deviceClient);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            if (receiveDataStatus)
            {
                string waterLevelBuff = messageData;
                Debug.WriteLine(waterLevelBuff);
                if(waterLevelBuff.Length > 100)
                {
                    dynamic data = JObject.Parse(waterLevelBuff);
                    waterLevel.guid = data.guid;
                    waterLevel.measurename1 = data.measurename1;
                    waterLevel.unitofmeasure1 = data.unitofmeasure1;
                    waterLevel.value1 = data.value1;
                    waterLevel.measurename2 = data.measurename2;
                    waterLevel.unitofmeasure2 = data.unitofmeasure2;
                    waterLevel.value2 = data.value2;
                }

                if ((waterLevel.guid.Equals("WL2016-0000-0001-0001-000000002")) && (waterLevel.measurename1.Equals("WaterLevel")))
                {
                    switch (Int32.Parse(waterLevel.value1))
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
                if ((waterLevel.guid.Equals("WL2016-0000-0001-0001-000000002")) && (waterLevel.measurename2.Equals("MotorStatus")))
                {
                    if (Int32.Parse(waterLevel.value2) == 0)
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

            }
        }

        static async void ReceiveCommands(DeviceClient deviceClient)
        {
            Debug.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
           

            while (true)
            {
                receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Debug.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                    receiveDataStatus = true;
                    await deviceClient.CompleteAsync(receivedMessage);
                }

                //  Note: In this sample, the polling interval is set to 
                //  10 seconds to enable you to see messages as they are sent.
                //  To enable an IoT solution to scale, you should extend this //  interval. For example, to scale to 1 million devices, set 
                //  the polling interval to 25 minutes.
                //  For further information, see
                //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

    }
}
