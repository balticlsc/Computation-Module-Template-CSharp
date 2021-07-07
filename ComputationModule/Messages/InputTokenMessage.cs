using System.Collections.Generic;

namespace ComputationModule.Messages
{
    public class InputTokenMessage
    {
        public string MsgUid { get; set; }

        public string PinName { get; set; }

        public string AccessType { get; set; }

        public string Values { get; set; }

        public IEnumerable<SeqToken> TokenSeqStack { get; set; }
    }
}