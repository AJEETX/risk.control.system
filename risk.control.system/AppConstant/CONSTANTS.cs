namespace risk.control.system.AppConstant
{
    public class CONSTANTS
    {
        public const string COUNTRY_AU = "au";
        public const string COUNTRY_IN = "in";
        public class CASE_STATUS
        {
            public const string INITIATED = "INITIATED";
            public const string INPROGRESS = "INPROGRESS";
            public const string FINISHED = "FINISHED";

            public class CASE_SUBSTATUS
            {
                public const string UPLOAD_IN_PROGRESS = "PENDING";
                public const string UPLOAD_COMPLETED = "UPLOADED";
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
                public const string WITHDRAWN_BY_AGENCY= "DECLINED";
            }
        }
        public const string UNDERWRITING = "underwriting";
        public const string CLAIM = "claim";

        public class LOCATIONS
        {
            //public const string AGENT_PHOTO = "AGENT_PHOTO";
            public const string EMPLOYMENT_ADDRESS = "EMPLOYMENT_ADDRESS";
            public const string CHEMIST_ADDRESS = "CHEMIST_ADDRESS";
            public const string LA_ADDRESS = "VERIFIER_ADDRESS";
            public const string BUSINESS_ADDRESS = "BUSINESS_ADDRESS";
            //public const string VERIFIER_PHOTO = "VERIFIER";

            //public const string CUSTOMER_ADDRESS = "CUSTOMER_ADDRESS";
            //public const string CUSTOMER_PHOTO = "CUSTOMER";
            //public const string AGENT_CUSTOMER_PHOTO = "AGENT_CUSTOMER";
            
            //public const string BENEFICIARY_ADDRESS = "BENEFICIARY_ADDRESS";
            //public const string BENEFICIARY_PHOTO = "BENEFICIARY";
            //public const string AGENT_BENEFICIARY_PHOTO = "AGENT_BENEFICIARY";
            
            public const string HOSPITAL_ADDRESS = "HOSPITAL";
            public const string CEMETERY_ADDRESS = "CEMETERY_ADDRESS ";
            public const string POLICE_ADDRESS = "POLICE_STATION";
            public const string ANGANWAADI_ADDRESS = "ANGANWAADI";
        }
    }
}