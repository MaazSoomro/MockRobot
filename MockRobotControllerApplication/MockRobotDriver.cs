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

        private TcpClient tcpClient;
        private NetworkStream netStream;

        private readonly Dictionary<EProcesses, string> processes;


        internal MockRobotDriver()
        {
            port = 1000;

            ConnectionState = EConnectionState.Disconnected;
            LastCommand = EProcesses.None;

            processes = new Dictionary<EProcesses, string>
            {
                { EProcesses.Homing, "home"},
                { EProcesses.Picking, "pick" },
                { EProcesses.Placing, "place" },
            };
        }

        public event EventHandler CommandFinishedSuccessfully;
        public event EventHandler CommandTerminatedWithError;

        public EConnectionState ConnectionState { get; private set; }
        public EProcesses LastCommand { get; private set; }
        public ESubState SubState { get; private set; }

        public bool Connect(string ip)
        {
            ConnectionState = EConnectionState.Connecting;

            tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);

            netStream = tcpClient.GetStream();

            if (tcpClient.Connected)
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

        public void Home()
        {
            var parameters = new string[] { "" };
            var processID = ProcessCommand(EProcesses.Homing, parameters);

            if (processID > 0)
            {
                LastCommand = (EProcesses)processID;
            }
        }

        internal void cycle()
        {
            SubState = StatusCommand((int)LastCommand);

            if (SubState == ESubState.InProgress)
            {
                // Process is running
            }
            if (SubState == ESubState.FinishedSuccessfully)
            {
                OnCommandFinishedSuccessfully();
            }
            else if (SubState == ESubState.TerminatedWithError)
            {
                OnCommandTerminatedWithError();
            }
        }

        private int ProcessCommand(EProcesses process, string[] parameters)
        {
            string commandName = processes[process];

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
            if (!tcpClient.Connected)
            {
                ConnectionState = EConnectionState.Disconnected;
                return "-1";      // assuming that -1 is not reserved
            }

            string parameters_flattened = string.Join("%", parameters);
            string command = $"{commandName}%{parameters_flattened}";

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);
            netStream.Write(commandBytes, 0, commandBytes.Length);

            byte[] responseBytes = new byte[1024];
            int bytesRead = netStream.Read(responseBytes, 0, responseBytes.Length);
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
    public enum EProcesses
    {
        None,
        Homing=10,
        Picking=20,
        Placing=30,
    }
}
