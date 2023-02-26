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

        public event EventHandler CommandFinishedSuccessfully;
        public event EventHandler CommandTerminatedWithError;

        public EConnectionState ConnectionState { get; private set; }
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

        }

        public void Home()
        {
            var parameters = new string[] { "" };
            var processID = ProcessCommand(EProcessId.Home, parameters);

            if (processID > 0)
            {
                LastProcessId = (EProcessId)processID;
                //StatusCommand(processID);
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
                //StatusCommand(processID);
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
                //StatusCommand(processID);
            }
        }

        internal void cycle()
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
                    // Control cannot reach here
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
                if (processID < 0)
                {
                    return -2;      // another process running
                }
                else
                {
                    return processID;
                }
            }
            else
            {
                return -3;          // invalid return value from command
            }

        }

        private ESubState StatusCommand(int processID)
        {
            string commandName = "status";
            string[] parameters = { $"processID" };
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
                if (returnValue == "-1")
                {
                    throw new Exception("Custom Exception: Connection broken down");
                }
                else
                {
                    throw new Exception("Custom exception: A problem with the response from status command");
                }
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
                return "-1";      // assuming that -1 is not reserved
            }

            string parameters_flattened = string.Join("%", parameters);
            string command = $"{commandName}%{parameters_flattened}";

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);
            m_NetStream.Write(commandBytes, 0, commandBytes.Length);

            byte[] responseBytes = new byte[1024];
            int bytesRead = m_NetStream.Read(responseBytes, 0, responseBytes.Length);
            string response = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);

            string parsedResponse = ParseResponse(response);

            return parsedResponse;
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
        Loc1,
        Loc2,
        Loc3,
    }
}
