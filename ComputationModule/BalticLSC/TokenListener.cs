namespace ComputationModule.BalticLSC
{
    public abstract class TokenListener
    {
        protected IJobRegistry Registry;
        protected IDataHandler Data;

        public TokenListener(IJobRegistry registry, IDataHandler data)
        {
            Registry = registry;
            Data = data;
        }

        /// 
        /// <param name="pinName"></param>
        public abstract void DataReceived(string pinName);

        /// 
        /// <param name="pinName"></param>
        public abstract void OptionalDataReceived(string pinName);

        public abstract void DataReady();

        public abstract void DataComplete();
    }
}