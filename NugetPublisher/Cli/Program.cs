using NugetPublisher.Application;
using NugetPublisher.Cli;
using NugetPublisher.Common;

try
{
    var options = Options.FromEnvironment();
    using var publisher = new Publisher(options);
    await publisher.ExecuteAsync();
    return 0;
}
catch (Exception ex)
{
    Logger.Error(ex.Message);
    Logger.Debug(ex.ToString());
    return 1;
}
