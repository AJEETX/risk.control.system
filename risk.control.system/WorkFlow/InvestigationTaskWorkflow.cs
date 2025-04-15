using risk.control.system.Models;

using WorkflowCore.Interface;

namespace risk.control.system.WorkFlow
{
    public class InvestigationTaskWorkflow : IWorkflow<InvestigationTask>
    {
        public string Id => "TaskWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<InvestigationTask> builder)
        {
            builder
            .StartWith<CaseDraftStep>()
            .Then<CaseCreateStep>()
            .If(data => data.IsReady2Assign) // Boolean condition
                .Do(then => then
                    .StartWith<CaseAssignToAgencyStep>())

            .If(data => !data.IsReady2Assign) // Else branch
                .Do(elsePath => elsePath
                    .StartWith<CaseCreateStep>())

                .If(data => data.AssignedToAgency)
                .Do(then => then.StartWith<CaseAssignToAgentStep>())
            .Then<CaseAgentReportSubmitted>()
            .If(data => data.InvestigationReport != null)
                .Do(then => then
                    .StartWith<CaseAgencyReportSubmitted>())
                .If(data => data.InvestigationReport == null)
                .Do(elsePath => elsePath
                    .StartWith<CaseReAssignedToAgentStep>())

            .If(data => data.AssignedToAgency)
            .Do(then => then
                    .StartWith<CaseApproved>())
                .If(data => !data.AssignedToAgency)
                .Do(elsePath => elsePath
                    .StartWith<CaseRejected>())
            ;
        }
    }
}
