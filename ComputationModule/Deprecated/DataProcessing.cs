using System.Collections.Generic;
using ComputationModule.BalticLSC;

namespace ComputationModule.Model
{
    public class DataProcessing
    {
        private PinConfiguration _configuration;

        public DataProcessing(PinConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Dictionary<string, string> Start(Dictionary<string, string> data)
        {
            //Perform computation
            return new Dictionary<string, string>();
        }
    }
}