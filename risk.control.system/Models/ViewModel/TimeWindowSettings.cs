﻿namespace risk.control.system.Models.ViewModel
{
    internal class TimeWindowSettings
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
    public static class FeatureFlags
    {
        public const string BaseVersion = "BaseVersion";
        public const string TrialVersion = "TrialVersion";
    }
}