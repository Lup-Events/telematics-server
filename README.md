# Lup.TelematicsServer
## Introduction
We use Telematics GPS trackers to keep an eye on the location of our equipment. Really we
want to know if a courier is running late delivering equipment, since that would have a huge
impact on us running an event!

So we're currently developing a library to receive telemetry directly from these trackers.

NOTE: This is still under development and currently pre-release.

## TLDR; How do I make it go?
```c#
static void Main() {
    // Instantiate the server
    var server = new TelematicsServer();
    
    // Define what happens when a tracker connects
    server.OnDeviceConnected += (sender, args) => {
        Console.WriteLine($"Device {args.Device.Serial} has connected from {args.RemoteAddressString}.");
    };
    
    // Define what happens when records are delivered
    server.OnRecordsReceived += (sender, args) => {
        Console.WriteLine($"Device {args.Device.Serial} has uploaded a set of records:");
        foreach (var record in args.Records) {
            Console.WriteLine($"  At {record.RTCTime} the following were raised because {record.Reason}:");
            foreach (var field in record.TrackingFields) {
                Console.WriteLine($"    Lat {field.PositionLatitude}, lng {field.PositionLongitude}");
            }
        }
    };
    
    // Start listening on the default port 8965 (configurable)
    server.Start();

    Console.WriteLine("Running. Press any key to stop.");
    Console.ReadKey(true);
}
```