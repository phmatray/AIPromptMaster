using System.Drawing;
using TickerQ.Utilities.Base;

namespace PromptMaster.BackgroundProcessor.Jobs;

public class MyJobs(ILogger<MyJobs> logger)
{
    // TickerQ 10.3 invokes jobs through a delegate that returns Task, so a `void`
    // job no longer binds - the generated factory fails to compile rather than at
    // runtime. Nothing here is asynchronous, hence Task.CompletedTask over async.
    [TickerFunction("CleanUpLogs", "*/1 * * * *")]
    public Task CleanUpLogs()
    {
        logger.LogInformation("Cleaning up logs");
        return Task.CompletedTask;
    }

    [TickerFunction("WithObject")]
    public Task WithObject(TickerFunctionContext<Point> tickerContext)
    {
        logger.LogInformation("Method called with object: {Point}", tickerContext.Request);
        return Task.CompletedTask;
    }
}
