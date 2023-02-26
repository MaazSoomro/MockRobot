using System;

namespace MockRobotControllerApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var RobotInterface = new MockRobotInterface();

            RobotInterface.ExecuteOperation("Pick", new string[] { "" }, new string[] { "" });
        }
    }
}
