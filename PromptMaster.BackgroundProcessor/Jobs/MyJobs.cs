using System.Drawing;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace PromptMaster.BackgroundProcessor.Jobs;

public class MyJobs(ILogger<MyJobs> logger)
{
    [TickerFunction("CleanUpLogs", "*/1 * * * *")]
    public void CleanUpLogs()
    {
        logger.LogInformation("Cleaning up logs");
    }

    [TickerFunction("WithObject")]
    public void WithObject(TickerFunctionContext<Point> tickerContext)
    {
        logger.LogInformation("Method called with object: {Point}", tickerContext.Request);
    }
}