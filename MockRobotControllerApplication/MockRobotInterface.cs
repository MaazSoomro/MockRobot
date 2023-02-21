using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockRobotControllerApplication
{
    public class MockRobotInterface
    {
        private MockRobotDriver driver;
        public MockRobotInterface()
        {
            driver = new MockRobotDriver();
        }

        public readonly List<string> AvailableOperations = new List<string> { "Pick", "Place", "Transfer" };

        public void OpenConnection(string IPAddress)
        {
            driver.Connect(IPAddress);
        }

        public void Initialize()
        {
            driver.Home();
        }

        public void ExecuteOperation(string operation, string[] parameterNames, string[] parameterValues)
        {

        }

        public void Abort()
        {

        }

        public void cycle()
        {
            driver.cycle();

            switch (DriverState)
            {
                default:
                    break;
            }
        }
    }

    public enum EDriverState
    {
        Uninitialized,
        Initializing,
        Idle,
        Picking,
        Placing,
    }
}
