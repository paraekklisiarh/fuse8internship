using Fuse8_ByteMinds.SummerSchool.PublicApi;
using Microsoft.AspNetCore;

var webHost = Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(
        webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
    .Build();

await webHost.RunAsync();
