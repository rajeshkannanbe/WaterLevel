using System;
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

namespace WaterLevelSerialApp
{
    public sealed partial class SerialAdruino
    {
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        private string arduioSerialData;
        private DeviceInformation[] entry = new DeviceInformation[3];
        private int count = 0;
        public bool serialDataAvilable = false;
        private CancellationTokenSource ReadCancellationTokenSource;


        private async void CheckSerialConnection()
        {

            try
            {
                Debug.WriteLine("CheckSerialConnection init");
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                Debug.WriteLine("dis.Count {0}", dis.Count);
                count = dis.Count;
                if (dis.Count <= 0)
                {
                    Debug.WriteLine("ERROR:: NO Serial Connection {0}\r\n", dis.Count);
                    return;
                }


                for (int i = 0; i < dis.Count; i++)
                {
                    entry[i] = dis[i];
                    Debug.WriteLine("configSerialConnection Start {0}", i);
                    //Task.Delay(100).Wait();
                    //configSerialConnection(dis[i]).Wait();
                    Debug.WriteLine("configSerialConnection End");
                    //Debug.WriteLine("dis[0] {0}",dis[i].Id);
                    //serialPort = await SerialDevice.FromIdAsync(dis[i].Id);
                    //if (serialPort != null)
                    //   break;

                }
                return;


            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception {0}", ex.Message);
            }
        }

        public async void InitSerial()
        {
            CheckSerialConnection();
        }

        public int NumSerialConnection()
        {
            return count;
        }
        public string GetPort()
        {
            return serialPort.PortName;
        }
        public string GetSerialData()
        {
            return arduioSerialData;
        }
        public async void connectSerial(int val)
        {
            configSerialConnection(entry[val]);
        }

        public SerialDevice GetSerialHandle()
        {
            return serialPort;
        }
        private async void configSerialConnection(DeviceInformation entry)
        {

            try
            {
                Debug.WriteLine("configSerialConnection Start {0}", serialPort);
                serialPort = await SerialDevice.FromIdAsync(entry.Id);
                if (serialPort != null)
                {

                    // Configure serial settings
                    serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.BaudRate = 9600;
                    serialPort.Parity = SerialParity.None;
                    serialPort.StopBits = SerialStopBitCount.One;
                    serialPort.DataBits = 8;
                    serialPort.Handshake = SerialHandshake.None;

                    //Dbg.Text = serialPort.PortName;
                    // Create cancellation token object to close I/O operations when closing the device
                    ReadCancellationTokenSource = new CancellationTokenSource();
                    Listen();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception {0}", ex.Message);
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    //Dbg.Text = "Reading task was cancelled, closing device and cleaning up";
                    Debug.WriteLine("Reading task was cancelled, closing device and cleaning up");
                    CloseDevice();
                }
                else
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                arduioSerialData = dataReaderObject.ReadString(bytesRead);
                serialDataAvilable = true;
                //rcvdText.Text = dataReaderObject.ReadString(bytesRead);
                //Dbg.Text = "bytes read successfully!";
                Debug.WriteLine("bytes read successfully!");
            }
        }

        // <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }
        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;
        }
    }
}
