using System;

namespace Lup.Telematics.Models {
	/// <summary>
	/// Message types received by device.
	/// </summary>
	public enum TransmitMessageTypes : Byte {
		HelloResponse = 1,
		CommitResponse = 6,
		AsyncMessageRequest = 20,
		AsyncSessionComplete = 23,
		TimeResponse = 31
	}
}