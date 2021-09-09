using System.IO;
using ComputationModule.BalticLSC;
using ComputationModule.Messages;
using Serilog;

namespace ComputationModule.Module
{
    public class MyTokenListener : TokenListener
    {
        public MyTokenListener(JobRegistry registry, DataHandler data) : base(registry, data) {}
        
        public override void DataReceived(string pinName)
        {
            // Place your code here:
            
        }

        public override void OptionalDataReceived(string pinName)
        {
            // Place your code here:

        }

        public override void DataReady()
        {
            // Place your code here:

        }

        public override void DataComplete()
        {
            // Place your code here:
            
        }
    }
}