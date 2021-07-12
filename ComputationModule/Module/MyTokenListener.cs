using ComputationModule.BalticLSC;

namespace ComputationModule.Module
{
    public class MyTokenListener : TokenListener
    {
        public MyTokenListener(IJobRegistry registry, IDataHandler data) : base(registry, data) {}
        
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