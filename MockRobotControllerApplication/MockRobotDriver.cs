using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MockRobotControllerApplication
{
    internal class MockRobotDriver
    {
        private readonly int port;

        private TcpClient m_TcpClient;
        private NetworkStream m_NetStream;
        private EConnectionState m_ConnectionState;
        private readonly Dictionary<EProcessId, string> m_Processes;


        internal MockRobotDriver()
        {
            port = 1000;

            ConnectionState = EConnectionState.Disconnected;
            LastProcessId = EProcessId.None;

            m_Processes = new Dictionary<EProcessId, string>
            {
                { EProcessId.Home, "home"},
                { EProcessId.Pick, "pick" },
                { EProcessId.Place, "place" },
            };
        }

        public event EventHandler ConnectionStateChanged;
        public event EventHandler CommandExecutionStarted;
        public event EventHandler CommandFinishedSuccessfully;
        public event EventHandler CommandTerminatedWithError;

        public event EventHandler<EProcessId> ProcessAlreadyRunning;

        public EConnectionState ConnectionState
        {
            get => m_ConnectionState;
            private set
            {
                if (m_ConnectionState != value)
                {
                    m_ConnectionState = value;
                    ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public EProcessId LastProcessId { get; private set; }
        public ESubState SubState { get; private set; }

        public bool Connect(string ip)
        {
            ConnectionState = EConnectionState.Connecting;

            m_TcpClient = new TcpClient();
            m_TcpClient.Connect(ip, port);

            m_NetStream = m_TcpClient.GetStream();

            if (m_TcpClient.Connected)
            {
                ConnectionState = EConnectionState.Connected;
                return true;
            }
            else
            {
                ConnectionState = EConnectionState.Disconnected;
                return false;
            }
        }

        public void Disconnect()
        {
            if (m_NetStream.CanRead)
            {
                // Read the server message into a byte buffer.
                byte[] bytes = new byte[1024];
                m_NetStream.Read(bytes, 0, 1024);
                //Convert the server's message into a string and display it.
                string data = Encoding.UTF8.GetString(bytes);
                Console.WriteLine("Server sent message: {0}", data);
            }

            m_NetStream.Close();
            m_TcpClient.Close();
        }

        public void Home()
        {
            var parameters = new string[] { "" };
            var processID = ProcessCommand(EProcessId.Home, parameters);

            if (processID > 0)
            {
                LastProcessId = (EProcessId)processID;
                CommandExecutionStarted?.Invoke(this, EventArgs.Empty);
                //StatusCommand(processID);
            }
            else
            {
                ProcessAlreadyRunning?.Invoke(this, LastProcessId);
            }
        }

        public void Pick(ELocationId sourceLocation)
        {
            int locId = (int)sourceLocation;
            var parameters = new string[] { locId.ToString() };
            var processID = ProcessCommand(EProcessId.Pick, parameters);

            if (processID > 0)
            {
                LastProcessId = (EProcessId)processID;
                CommandExecutionStarted?.Invoke(this, EventArgs.Empty);
                //StatusCommand(processID);
            }
            else
            {
                ProcessAlreadyRunning?.Invoke(this, LastProcessId);
            }
        }

        public void Place(ELocationId destLocation)
        {
            int locId = (int)destLocation;
            var parameters = new string[] { locId.ToString() };
            var processID = ProcessCommand(EProcessId.Place, parameters);

            if (processID > 0)
            {
                LastProcessId = (EProcessId)processID;
                CommandExecutionStarted?.Invoke(this, EventArgs.Empty);
                //StatusCommand(processID);
            }
            else
            {
                ProcessAlreadyRunning?.Invoke(this, LastProcessId);
            }
        }

        internal void cycle()
        {
            //if (m_TcpClient != null)
            //{
            //    ConnectionState = m_TcpClient.Connected == true ? EConnectionState.Connected : EConnectionState.Disconnected;
            //}

            if (ConnectionState == EConnectionState.Connected)
            {
                if (SubState == ESubState.InProgress)
                {
                    if (SubState == ESubState.InProgress)
                    {
                        // Process is running
                    }
                    else if (SubState == ESubState.FinishedSuccessfully)
                    {
                        OnCommandFinishedSuccessfully();
                    }
                    else if (SubState == ESubState.TerminatedWithError)
                    {
                        OnCommandTerminatedWithError();
                    }
                    else
                    {
                        // Communication error handling
                    }
                }
            }

            SubState = StatusCommand((int)LastProcessId);
        }

        private int ProcessCommand(EProcessId process, string[] parameters)
        {
            string commandName = m_Processes[process];

            var returnValue = Command(commandName, parameters);

            var isSuccessful = int.TryParse(returnValue, out int processID);

            if (isSuccessful)
            {
                return processID;
            }
            else
            {
                throw new Exception($"Invalid response received from command {process}.");
            }

        }

        private ESubState StatusCommand(int processID)
        {
            string commandName = "status";
            string[] parameters = { $"processID" };
            try
            {
                var returnValue = Command(commandName, parameters);

                if (returnValue == "In Progress")
                {
                    return ESubState.InProgress;
                }
                else if (returnValue == "Finished Successfully")
                {
                    return ESubState.FinishedSuccessfully;
                }
                else if (returnValue == "Terminated With Error")
                {
                    return ESubState.TerminatedWithError;
                }
                else
                {
                    if (returnValue == "-1" || returnValue == "-2" || returnValue == "-3")
                    {
                        //throw new Exception("Custom Exception: Connection broken down");
                        ConnectionState = EConnectionState.Disconnected;
                        return ESubState.CommunicationError;
                    }
                    else
                    {
                        throw new Exception($"Invalid response received during StatusCommand. Response: {returnValue}");
                    }
                }
            }
            catch (Exception)
            {
                ConnectionState = EConnectionState.Disconnected;
                throw;
            }
        }


        /// <summary>
        /// Low level communication with MockRobot
        /// </summary>
        /// <param name="commandName">command name to be executed</param>
        /// <param name="parameters">optional input parameters</param>
        /// <returns>processID or the status</returns>
        private string Command(string commandName, string[] parameters)
        {
            if (!m_TcpClient.Connected)
            {
                ConnectionState = EConnectionState.Disconnected;
                return "-1";      // Not connected
            }


            if (m_NetStream.CanWrite)
            {
                string parameters_flattened = string.Join("%", parameters);
                string command = $"{commandName}%{parameters_flattened}";

                byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                m_NetStream.Write(commandBytes, 0, commandBytes.Length);
            }
            else
            {
                return "-2";        // Cannot write
            }

            if (m_NetStream.CanRead)
            {
                byte[] responseBytes = new byte[1024];
                int bytesRead = m_NetStream.Read(responseBytes, 0, responseBytes.Length);
                string response = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);

                string parsedResponse = ParseResponse(response);

                return parsedResponse;
            }
            else
            {
                return "-3";        // Cannot read
            }

        }

        private string ParseResponse(string data)
        {
            // parsing logic goes here
            // remove garbage from data
            // TODO
            return data;
        }

        private void OnCommandFinishedSuccessfully()
        {
            CommandFinishedSuccessfully?.Invoke(this, EventArgs.Empty);
        }

        private void OnCommandTerminatedWithError()
        {
            CommandTerminatedWithError?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum EConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public enum ESubState
    {
        InProgress,
        FinishedSuccessfully,
        TerminatedWithError,
        CommunicationError,
    }

    /// <summary>
    /// Represenst process IDs
    /// </summary>
    public enum EProcessId
    {
        None,
        Home=10,
        Pick=20,
        Place=30,
    }

    /// <summary>
    /// Location as an enum
    /// </summary>
    public enum ELocationId
    {
        None,
        Loc1,
        Loc2,
        Loc3,
    }

    public enum EErrorId : int
    {
        None=0,
        NotConnected=-1,
        CannotWrite=-2,
        CannotRead=-3,
        InvalidReturnValue=-4,
    }
}
