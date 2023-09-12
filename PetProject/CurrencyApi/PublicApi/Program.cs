using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Microsoft.EntityFrameworkCore;

var webHost = Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(
        webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
    .Build();

await MigrateDatabase(webHost);

await webHost.RunAsync();


async Task MigrateDatabase(IHost webApplication)
{
    using var scope = webApplication.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (context == null)
        throw new Exception("Cannot initialize the database context");

    await context.Database.MigrateAsync();
}