using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockRobotControllerApplication
{
    public class MockRobotInterface
    {
        private MockRobotDriver m_Driver;
        private ELocationId m_SourceLoc;
        private ELocationId m_DestLoc;
        private EDriverState m_NextState;
        private bool m_IsTransfer;
        private bool m_IsCommandRunning;

        public MockRobotInterface()
        {
            m_Driver = new MockRobotDriver();
            m_Driver.ConnectionStateChanged += Driver_ConnectionStateChanged;
            m_Driver.CommandExecutionStarted += Driver_CommandExecutionStarted;
            m_Driver.CommandFinishedSuccessfully += Driver_CommandFinishedSuccessfully;
            m_Driver.CommandTerminatedWithError += Driver_CommandTerminatedWithError;

            DriverState = EDriverState.Uninitialized;

            m_SourceLoc = ELocationId.None;
            m_DestLoc = ELocationId.None;

            AvailableOperations = new Dictionary<string, Action<string[], string[]>>
            {
                {"Pick", Pick },
                {"Place", Place },
                {"Transfer", Transfer },
            };
        }

        public enum EDriverState
        {
            Uninitialized,
            Initializing,
            Idle,
            Picking,
            Placing,
        }

        public event EventHandler ConnectionStateChanged;
        public event EventHandler CommandFinishedSuccessfully;
        public event EventHandler ErrorOccurred;

        //public readonly List<string> AvailableOperations = new List<string> { "Pick", "Place", "Transfer" };
        private Dictionary<string, Action<string[], string[]>> AvailableOperations { get; }

        public EDriverState DriverState { get; private set; }

        public EConnectionState ConnectionState { get => m_Driver.ConnectionState; }

        public void OpenConnection(string IPAddress)
        {
            m_Driver.Connect(IPAddress);
        }

        public void Initialize()
        {
            if (ConnectionState == EConnectionState.Connected)
            {
                m_Driver.Home();
                m_NextState = EDriverState.Initializing;
            }
        }

        public void ExecuteOperation(string operation, string[] parameterNames, string[] parameterValues)
        {
            var isSuccessful = AvailableOperations.TryGetValue(operation, out var func);
            if (isSuccessful)
            {
                func(parameterNames, parameterValues);
            }
        }

        public void Abort()
        {
            m_Driver.Disconnect();
        }

        public void cycle()
        {
            m_Driver.cycle();

            if (m_IsCommandRunning)
            {
                OnCommandRunning();
            }
        }

        private void OnCommandRunning()
        {
            switch (DriverState)
            {
                case EDriverState.Uninitialized:
                    break;
                case EDriverState.Initializing:
                    break;
                case EDriverState.Idle:
                    break;
                case EDriverState.Picking:
                    break;
                case EDriverState.Placing:
                    break;
                default:
                    break;
            }
        }

        private void Pick(string[] parameterNames, string[] parameterValues)
        {
            if (parameterNames.Length != 1 || parameterValues.Length != 1)
            {
                // error handling
                return;
            }

            if (parameterNames[0] != "Source Location")
            {
                // error handling
                return;
            }

            var isSuccessful = int.TryParse(parameterValues[0], out var src);

            if (isSuccessful)
            {
                if (Enum.IsDefined(typeof(ELocationId), src))
                {
                    m_SourceLoc = (ELocationId)src;
                    m_NextState = EDriverState.Picking;
                    m_Driver.Pick(m_SourceLoc);
                }
                else
                {
                    // error handling
                    return;
                }
            }
        }

        private void Place(string[] parameterNames, string[] parameterValues)
        {
            if (parameterNames.Length != 1 || parameterValues.Length != 1)
            {
                // error handling
                return;
            }

            if (parameterNames[0] != "Destination Location")
            {
                // error handling
                return;
            }

            var isSuccessful = int.TryParse(parameterValues[0], out var dest);

            if (isSuccessful)
            {
                if (Enum.IsDefined(typeof(ELocationId), dest))
                {
                    m_SourceLoc = (ELocationId)dest;
                    m_NextState = EDriverState.Placing;
                    m_Driver.Pick(m_SourceLoc);
                }
                else
                {
                    // error handling
                    return;
                }
            }
        }

        private void Transfer(string[] parameterNames, string[] parameterValues)
        {
            if (parameterNames.Length != 2 || parameterValues.Length != 2)
            {
                // error handling
                return;
            }

            var src_i = Array.IndexOf(parameterNames, "Source Location");
            var dest_i = Array.IndexOf(parameterNames, "Destination Location");

            if(src_i == -1 || dest_i == -1)
            {
                // error handling
                return;
            }

            var isSuccessful = int.TryParse(parameterValues[src_i], out var src);

            if (isSuccessful)
            {

                if (Enum.IsDefined(typeof(ELocationId), src))
                {
                    m_SourceLoc = (ELocationId)src;
                    m_NextState = EDriverState.Picking;
                    m_Driver.Pick(m_SourceLoc);

                    m_IsCommandRunning = true;
                }
                else
                {
                    // error handling
                    return;
                }

                isSuccessful = int.TryParse(parameterValues[dest_i], out var dest);

                if (isSuccessful)
                {
                    if (Enum.IsDefined(typeof(ELocationId), dest))
                    {
                        m_DestLoc = (ELocationId)dest;
                        m_IsTransfer = true;
                    }
                    else
                    {
                        return;
                    }
                }

            }

        }

        private void Driver_CommandTerminatedWithError(object sender, EventArgs e)
        {
            m_NextState = EDriverState.Uninitialized;
            DriverState = m_NextState;

            m_IsCommandRunning = false;
            m_IsTransfer = false;

            ErrorOccurred?.Invoke(this, EventArgs.Empty);
        }

        private void Driver_CommandExecutionStarted(object sender, EventArgs e)
        {
            DriverState = m_NextState;

            if (m_IsTransfer && DriverState == EDriverState.Picking)
            {
                m_NextState = EDriverState.Placing;
            }
            else
            {
                m_NextState = EDriverState.Idle;
            }
        }

        private void Driver_CommandFinishedSuccessfully(object sender, EventArgs e)
        {
            if (m_NextState == EDriverState.Placing)
            {
                m_Driver.Place(m_DestLoc);
                m_IsTransfer = false;
                m_IsCommandRunning = true;
            }
            else
            {
                m_IsCommandRunning = false;
                DriverState = m_NextState;

                CommandFinishedSuccessfully?.Invoke(this, EventArgs.Empty);
            }

        }

        private void Driver_ConnectionStateChanged(object sender, EventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
