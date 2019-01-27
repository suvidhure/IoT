using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace simulated_device
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;
        private readonly static string s_connectionString = "HostName=suvi-iot-hub.azure-devices.net;DeviceId=device-01;SharedAccessKey=glRgnhm+fCMy1muB/m25Hs3skvR0a6gWrcZ2pFj2cI0=";
        private static int s_telemetryInterval = 1; // Seconds

         private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            // Check the payload is a single integer value
            if (Int32.TryParse(data.Replace("\"",""), out s_telemetryInterval))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Telemetry interval set to {0} seconds", data);
                Console.ResetColor();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }
        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemprature=20;
            double minhumidity=60;
            Random rand=new Random();

            while(true)
            {
                double currminTemprature=minTemprature+ rand.NextDouble() *15;
                double currhumidity=minhumidity+rand.NextDouble() * 20;

                var telemetryDataPoint= new
                {
                    temprature=currminTemprature,
                    humidity=currhumidity
                };
                var messagepoint=JsonConvert.SerializeObject(telemetryDataPoint);
                var message=new Message(Encoding.ASCII.GetBytes(messagepoint));

                message.Properties.Add("tempratureAlert",(currminTemprature>30?"true":"false"));
                await s_deviceClient.SendEventAsync(message);

                Console.WriteLine("{0} > sending message {1}",DateTime.Now,messagepoint);

                await Task.Delay(s_telemetryInterval*1000);

            }

        }
        static void Main(string[] args)
        {
           Console.WriteLine("IoT Hub Quickstarts - Simulated device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);

            // Create a handler for the direct method call
            s_deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}
