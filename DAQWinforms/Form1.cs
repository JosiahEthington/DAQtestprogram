using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using LiveCharts.Configurations;
using System.Web.Script.Serialization;
//using System.Threading;


namespace DAQWinforms
{
    public class MeasureModel
    {
        public System.DateTime DateTime { get; set; }
        public double Value { get; set; }
    }
    public partial class Form1 : Form
    {
        public SensorObj sensor1;
        public Form1()
        {
            InitializeComponent();
            var mapper = Mappers.Xy<MeasureModel>()
    .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
    .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the ChartValues property will store our values array
            ChartValues = new ChartValues<MeasureModel>();
            cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = ChartValues,
                    PointGeometrySize = 18,
                    StrokeThickness = 4
                }
            };
            cartesianChart1.AxisX.Add(new Axis
            {
                DisableAnimations = true,
                LabelFormatter = value => new System.DateTime((long)value).ToString("mm:ss"),
                Separator = new Separator
                {
                    Step = TimeSpan.FromSeconds(1).Ticks
                }
            });

            SetAxisLimits(System.DateTime.Now);
            Timer = new Timer
            {
                Interval = 1900
            };
            //The next code simulates data changes every 500 ms
            Timer.Tick += TimerOnTick;
            Timer.Start();
            //Thread listenThread;
            //listenThread = new Thread(new ThreadStart(StartListener));
            //listenThread.Start();
        }
        public System.Windows.Forms.Timer Timer { get; set; }

        public ChartValues<MeasureModel> ChartValues { get; set; }

        private void SetAxisLimits(System.DateTime now)
        {
            cartesianChart1.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
            cartesianChart1.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(60).Ticks; //we only care about the last 8 seconds
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            JavaScriptSerializer ser = new JavaScriptSerializer();
            var now = System.DateTime.Now;
            bool searching = true;
            SensorObj temp = new SensorObj();
            //double R = 0;
            try
            {
                while (searching)
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    sensor1 = ser.Deserialize<SensorObj>(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    if (sensor1.sensorID == "1" && sensor1.sensor == "temp")
                    {
                        ChartValues.Add(new MeasureModel
                        {
                            DateTime = now,
                            Value = sensor1.value
                        });
                        searching = false;
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }


            SetAxisLimits(now);

            //lets only use the last 30 values
            if (ChartValues.Count > 30) ChartValues.RemoveAt(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartListener();
            //Sensor temp = new Sensor;
            //temp.sensor = "something";
            
        }
        private const int listenPort = 8888;

        private void StartListener()
        {
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            JavaScriptSerializer ser = new JavaScriptSerializer();

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);
                    string temp = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine(temp);
                    sensor1 = ser.Deserialize<SensorObj>(temp);
                    //Console.WriteLine("Sensor:" + sensor1.ToString());

                    //TODO: Change this to reflect code recieved from UDP.
                    //Console.WriteLine($"Received broadcast from {groupEP} :");
                    //Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }
    }
    public class SensorObj
    {
        public string sensor { get; set; }
        public string sensorID { get; set; }
        public double value { get; set; }
        public override int GetHashCode()
        {
            return (sensor + sensorID).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }
            if(obj.GetType() == this.GetType())
            {
                Equals((SensorObj)obj);
            }
            return false;
        }
        public override string ToString()
        {
            return "Sensor: " + sensor + ";" + " SensorID: " + sensorID + ";" + " Value: " + value;
        }

        public bool Equals(SensorObj other)
        {
            if(this.sensor == other.sensor && this.sensorID == other.sensorID)
            {
                return true;
            }
            return false;
        }
    }

}
