﻿using System.Collections.Generic;

namespace ComputationModule.Messages
{
    public class TokensAck {
        public List<string> MsgUids { get; set; }
        public string SenderUid { get; set; }
        public string Note { get; set; }
        public bool IsFailed { get; set; }
        public bool IsFinal { get; set; }
    }
}