namespace ComputationModule.Messages
{
    public class JobStatus
    {
        public Status Status { get; set; } = Status.Idle;
        public long JobProgress { get; set; } = -1;
        public string JobInstanceUid { get; set; }

    }
}
