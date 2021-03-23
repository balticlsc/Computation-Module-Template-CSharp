namespace ComputationModule.Model.BalticDataModel
{
    public class JobStatus
    {
        public ComputationStatus Status { get; set; } = ComputationStatus.Unknown;
        public long JobProgress { get; set; } = -1;
        public string JobInstanceUid { get; set; }

    }
}
