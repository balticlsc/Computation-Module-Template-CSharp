using ComputationModule.BalticLSC;

namespace ComputationModule.Model
{
    public class DataStoreProxy
    {
        private PinConfiguration _configuration;

        public DataStoreProxy(PinConfiguration configuration)
        {
            _configuration = configuration;
        }

        public short CheckDataConnections()
        {
            //Check connections to data stores, return non-zero value if connection fails
            return 0;
        }
    }
}