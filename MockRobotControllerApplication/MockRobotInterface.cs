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
        private EDriverState m_NextCommandToExecute;
        private bool m_IsTransfer;

        public MockRobotInterface()
        {
            m_Driver = new MockRobotDriver();
            m_Driver.ConnectionStateChanged += Driver_ConnectionStateChanged;
            m_Driver.CommandExecutionStarted += M_Driver_CommandExecutionStarted;
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

        //public readonly List<string> AvailableOperations = new List<string> { "Pick", "Place", "Transfer" };
        public Dictionary<string, Action<string[], string[]>> AvailableOperations { get; private set; }

        public EDriverState DriverState { get; private set; }

        public EConnectionState ConnectionState { get => m_Driver.ConnectionState; }

        public void OpenConnection(string IPAddress)
        {
            m_Driver.Connect(IPAddress);
        }

        public void Initialize()
        {
            m_Driver.Home();
            DriverState = EDriverState.Initializing;
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

        private void Pick(string[] paramenterNames, string[] parameterValues)
        {
            Console.WriteLine("Pick called!");
        }

        private void Place(string[] paramenterNames, string[] parameterValues)
        {

        }

        private void Transfer(string[] paramenterNames, string[] parameterValues)
        {

        }

        private void Driver_CommandTerminatedWithError(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void M_Driver_CommandExecutionStarted(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Driver_CommandFinishedSuccessfully(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Driver_ConnectionStateChanged(object sender, EventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
