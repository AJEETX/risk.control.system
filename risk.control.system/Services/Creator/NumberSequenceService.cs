using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.Services.Creator
{
    public interface INumberSequenceService
    {
        Task<string> GetNumberSequence(string module);
        Task SaveNumberSequence(string module);
    }
    internal class NumberSequenceService : INumberSequenceService
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<NumberSequenceService> logger;

        public NumberSequenceService(ApplicationDbContext context, ILogger<NumberSequenceService> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        public async Task<string> GetNumberSequence(string module)
        {
            string result = "";
            try
            {
                int counter = 0;

                NumberSequence numberSequence = await context.NumberSequence.FirstOrDefaultAsync(x => x.Module.Equals(module));

                if (numberSequence is null)
                {
                    numberSequence = new NumberSequence();
                    numberSequence.Module = module;
                    Interlocked.Increment(ref counter);
                    numberSequence.LastNumber = counter;
                    numberSequence.NumberSequenceName = module;
                    numberSequence.Prefix = module;
                }
                else
                {
                    counter = numberSequence.LastNumber;
                    Interlocked.Increment(ref counter);
                    numberSequence.LastNumber = counter;
                }

                result = numberSequence.Prefix + counter.ToString().PadLeft(5, '0');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred.");
                throw;
            }
            return result;
        }

        public async Task SaveNumberSequence(string module)
        {
            NumberSequence numberSequence = await context.NumberSequence.FirstOrDefaultAsync(x => x.Module.Equals(module));
            int counter = 0;

            if (numberSequence is null)
            {
                numberSequence = new NumberSequence();
                numberSequence.Module = module;
                Interlocked.Increment(ref counter);
                numberSequence.LastNumber = counter;
                numberSequence.NumberSequenceName = module;
                numberSequence.Prefix = module;
                context.Add(numberSequence);
            }
            else
            {
                counter = numberSequence.LastNumber;
                Interlocked.Increment(ref counter);
                numberSequence.LastNumber = counter;
                context.Update(numberSequence);
            }
        }
    }
}
