using risk.control.system.Models;

using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace risk.control.system.WorkFlow
{
    public class CaseWithdrawStep : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = context.Workflow.Data as InvestigationTask;
            if (data != null)
            {
                data.Status = "CaseWithdrawStep"; // ✅ This now works
            }
            return ExecutionResult.Next();
        }
    }
}
