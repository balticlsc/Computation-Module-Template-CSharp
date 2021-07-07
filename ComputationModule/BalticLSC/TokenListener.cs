namespace ComputationModule.BalticLSC
{
    public abstract class TokenListener
    {
        protected JobRegistry Registry;
        protected DataHandler Data;

        public TokenListener()
        {
        }

        /// 
        /// <param name="pinName"></param>
        public void DataReceived(string pinName)
        {
        }

        /// 
        /// <param name="pinName"></param>
        public void OptionalDataReceived(string pinName)
        {
        }

        public void DataReady()
        {
        }

        public void DataComplete()
        {
        }
    }
}