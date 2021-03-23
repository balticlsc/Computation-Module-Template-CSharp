namespace ComputationModule.Model.BalticDataModel
{
    public enum ComputationStatus
    {
        Idle = 0,
        Working = 1,
        Completed = 2,
        Failed = 3,
        Rejected = 4,
        Aborted = 5,
        Neglected = 6,
        Scheduled = 7,
        Unknown = 8,
    }
}
