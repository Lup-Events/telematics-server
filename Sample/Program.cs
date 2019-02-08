using System;
using Lup.Telematics;

namespace Sample {
	class Program {
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
	}
}