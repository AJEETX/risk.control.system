namespace risk.control.system.AppConstant
{
    public static class CONSTANTS
    {
        public const string COUNTRY_AU = "au";
        public const string COUNTRY_IN = "in";
        public const string EmptyNull = "null/empty";
        public const string NullInvalid = "null/invalid";
        public const string ValidDateFormat = "dd-MM-yyyy";

        public static class CASE_STATUS
        {
            public const string INITIATED = "INITIATED";
            public const string INPROGRESS = "INPROGRESS";
            public const string FINISHED = "FINISHED";

            public static class CASE_SUBSTATUS
            {
                public const string UPLOAD_IN_PROGRESS = "PENDING";
                public const string UPLOAD_COMPLETED = "UPLOADED";
                public const string UPLOAD_ERR = "UPLOAD_ERR";
                public const string DRAFTED_BY_CREATOR = "DRAFTED";
                public const string CREATED_BY_CREATOR = "CREATED";
                public const string EDITED_BY_CREATOR = "EDITED";
                public const string ASSIGNED_TO_ASSIGNER = "ASSIGNED";
                public const string ALLOCATED_TO_VENDOR = "ALLOCATED";
                public const string ASSIGNED_TO_AGENT = "TASKED";
                public const string SUBMITTED_TO_SUPERVISOR = "INVESTIGATED";
                public const string SUBMITTED_TO_ASSESSOR = "SUBMITTED";
                public const string REPLY_TO_ASSESSOR = "REPLY";
                public const string APPROVED_BY_ASSESSOR = "APPROVED";
                public const string REJECTED_BY_ASSESSOR = "REJECTED";
                public const string REQUESTED_BY_ASSESSOR = "REQUESTED";
                public const string REASSIGNED_TO_ASSIGNER = "REASSIGNED";
                public const string WITHDRAWN_BY_COMPANY = "WITHDRAWN";
                public const string WITHDRAWN_BY_AGENCY = "DECLINED";
            }
        }
        public readonly static string[] CreatedAndDraftStatuses = new[] {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER
            };
        public static readonly string[] ActiveSubStatuses = new[]
            {
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_IN_PROGRESS,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.UPLOAD_COMPLETED,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.DRAFTED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.EDITED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.CREATED_BY_CREATOR,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.ASSIGNED_TO_ASSIGNER,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_COMPANY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.WITHDRAWN_BY_AGENCY,
                CONSTANTS.CASE_STATUS.CASE_SUBSTATUS.REQUESTED_BY_ASSESSOR
            };
        public const string UNDERWRITING = "underwriting";
        public const string CLAIM = "claim";

        public static class LOCATIONS
        {
            public const string EMPLOYMENT_ADDRESS = "EMPLOYMENT";
            public const string CHEMIST_ADDRESS = "CHEMIST";
            public const string LA_ADDRESS = "LA ADDRESS";
            public const string BENEFICIARY_ADDRESS = "BENEFICIARY ADDRESS";
            public const string BUSINESS_ADDRESS = "BUSINESS";

            public const string HOSPITAL_ADDRESS = "HOSPITAL";
            public const string CEMETERY_ADDRESS = "CEMETERY";
            public const string POLICE_ADDRESS = "POLICE STATION";
            public const string ANGANWAADI_ADDRESS = "ANGANWAADI";
        }

        public const string POLICY_IMAGE = "policy.jpg";
        public const string CUSTOMER_IMAGE = "customer.jpg";
        public const string BENEFICIARY_IMAGE = "beneficiary.jpg";
    }
    
}