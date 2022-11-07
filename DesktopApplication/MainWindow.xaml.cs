using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using Assignment2Part2.Models;
using RestSharp;
using Newtonsoft.Json;
using IronPython.Hosting;
using RemotingService;
using System.ServiceModel;
using DataTypes;
using System.Security.Cryptography;

namespace Desktop_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string script;
        public static Client thisClient;
        public MainWindow()
        {
            InitializeComponent();
        }
        public async void NetworkingThread()
        {
            while (true)
            {
                Task<bool> task = new Task<bool>(LookForClientsTask);
                task.Start();
                bool done = await task;
                if(done == true)
                {
                    await Task.Delay(50);
                }
                await Task.Delay(1000);
            }
        }
        public bool LookForClientsTask()
        {
                RestClient restClient = new RestClient("http://localhost:53863/");
                RestRequest request = new RestRequest("api/clients/", Method.Get);
                RestResponse resp = restClient.Get(request);
                List<Client> returnClients = JsonConvert.DeserializeObject<List<Client>>(resp.Content);
                bool doneJob = false;
                foreach (Client client in returnClients)
                {
                if (client.Id != thisClient.Id)
                {
                    try {
                        ChannelFactory<RemotingInterface> foobFactory;
                        NetTcpBinding tcp = new NetTcpBinding();
                        string URL = "net.tcp://" + client.Address + ":" + client.Port + "/RemotingService";
                        foobFactory = new ChannelFactory<RemotingInterface>(tcp, URL);
                        RemotingInterface foob = foobFactory.CreateChannel();
                        List<int> jobs = foob.GetUnfinishedJobs();

                        //Downloading jobs
                        foreach (int jobIndex in jobs)
                        {
                            Job job = foob.GetJob(jobIndex);
                            try
                            {
                                //do job
                                Microsoft.Scripting.Hosting.ScriptEngine engine = Python.CreateEngine();
                                Microsoft.Scripting.Hosting.ScriptScope scope = engine.CreateScope();
                                if (generateHash(job.script).SequenceEqual(job.hash))
                                {
                                    engine.Execute(base64Decode(job.script), scope);
                                }
                                else
                                {
                                    Console.WriteLine("Data Corrupted");
                                }
                                dynamic testFunction = scope.GetVariable("Start");
                                var result = testFunction();
                                foob.AddSoultion(result, jobIndex);


                                //indicate job completed
                                thisClient.JobsCompleted += 1;
                                request = new RestRequest("api/clients/{id}", Method.Put);
                                request.AddUrlSegment("id", thisClient.Id);
                                request.AddJsonBody(JsonConvert.SerializeObject(thisClient));
                                resp = restClient.Put(request);
                                doneJob = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Bad Client Code");
                            }

                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Client Closed");
                    }
                    }
                }
            return doneJob;
        }
        public async void ServerThread()
        {
            try
            {
                Task<ServiceHost> task = new Task<ServiceHost>(ServerTask);
                task.Start();
                ServiceHost host = await task;
                RestClient restClient = new RestClient("http://localhost:53863/");
                RestRequest request = new RestRequest("api/clients", Method.Post);
                request.AddJsonBody(JsonConvert.SerializeObject(thisClient));
                RestResponse resp = restClient.Post(request);
                while (true)
                {
                    await Task.Delay(5000);
                    thisClient.PingTime = DateTime.Now;
                    request = new RestRequest("api/clients/{id}", Method.Put);
                    request.AddUrlSegment("id", thisClient.Id);
                    request.AddJsonBody(JsonConvert.SerializeObject(thisClient));
                    resp = restClient.Put(request);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Wrong IP/Port Input");
            }
            //host.Close();
        }
        public ServiceHost ServerTask()
        {

            try
            {
                Remoting server = Remoting.Instance;
                ServiceHost host = new ServiceHost(server);
                NetTcpBinding tcp = new NetTcpBinding();
                host = new ServiceHost(typeof(Remoting));
                host.AddServiceEndpoint(typeof(RemotingInterface), tcp, "net.tcp://" + thisClient.Address + ":" + thisClient.Port + "/RemotingService");
                host.Open();
                return host;
            }
            catch(Exception e)
            {
                return null;
            }
        }

        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".txt"; // Required file extension 

            fileDialog.ShowDialog();
            FileStream fs = File.Open(fileDialog.FileName, FileMode.Open);
            using (var reader = new StreamReader(fs))
            {
                string line = null;
                ScriptBox.Text = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        ScriptBox.Text = ScriptBox.Text + line + "\n";
                    }
                }
            }
            fs.Close();
            script = ScriptBox.Text;
            SelectedFile.Content = fileDialog.FileName;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            ChannelFactory<RemotingInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();
            string URL = "net.tcp://" + thisClient.Address + ":" + thisClient.Port + "/RemotingService";
            foobFactory = new ChannelFactory<RemotingInterface>(tcp, URL);
            RemotingInterface foob = foobFactory.CreateChannel();
            string encodedData = base64Encode(ScriptBox.Text);
            foob.AddJob(new Job(encodedData, generateHash(encodedData)));
        }
        public string base64Encode(string s)
        {
            if(String.IsNullOrEmpty(s))
            {
                return s;
            }
            byte[] encodedBytes = Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(encodedBytes);
        }
        public byte[] generateHash(string s)
        {
            return SHA256.Create().ComputeHash(Convert.FromBase64String(s));
        }
        public string base64Decode(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }
            byte[] encodedBytes = Convert.FromBase64String(s);
            return Encoding.UTF8.GetString(encodedBytes);
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            thisClient = new Client();
            thisClient.Id = new Random().Next();
            thisClient.Address = HostIP.Text;
            thisClient.Port = HostPort.Text;
            thisClient.JobsCompleted = 0;
            thisClient.PingTime = DateTime.Now;
            HostIP.Visibility = Visibility.Hidden;
            HostPort.Visibility = Visibility.Hidden;
            StartServer.Visibility = Visibility.Hidden;
            NetworkingThread();
            ServerThread();
        }
    }
}
