namespace risk.control.system.AppConstant
{
    public class CONSTANTS
    {
        public class CASE_STATUS
        {
            public const string INITIATED = "INITIATED";
            public const string INPROGRESS = "INPROGRESS";
            public const string FINISHED = "FINISHED";

            public class CASE_SUBSTATUS
            {
                public const string CREATED_BY_CREATOR = "CREATED_BY_CREATOR";
                public const string ASSIGNED_TO_ASSIGNER = "ASSIGNED_TO_ASSIGNER";
                public const string ALLOCATED_TO_VENDOR = "ALLOCATED_TO_VENDOR";
                public const string ASSIGNED_TO_AGENT = "ASSIGNED_TO_AGENT";
                public const string SUBMITTED_TO_SUPERVISOR = "SUBMITTED_TO_SUPERVISOR";
                public const string SUBMITTED_TO_ASSESSOR = "SUBMITTED_TO_ASSESSOR";
                public const string APPROVED_BY_ASSESSOR = "APPROVED_BY_ASSESSOR";
                public const string REJECTED_BY_ASSESSOR = "REJECTED_BY_ASSESSOR";
                public const string REASSIGNED_TO_ASSIGNER = "REASSIGNED_TO_ASSIGNER";
                public const string WITHDRAWN_BY_COMPANY = "WITHDRAWN_BY_COMPANY";
            }
        }
    }
}
