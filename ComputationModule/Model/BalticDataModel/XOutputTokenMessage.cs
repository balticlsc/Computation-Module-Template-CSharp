namespace ComputationModule.Model.BalticDataModel
{ 
    public class XOutputTokenMessage
    {
        public string PinName { get; set; }
        public string SenderUid { get; set; }
        public string Values { get; set; }
        public string BaseMsgUid { get; set; }
        public bool IsFinal { get; set; }
    }
}
