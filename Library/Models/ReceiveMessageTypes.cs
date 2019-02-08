using System;

namespace Lup.Telematics.Models {
	/// <summary>
	/// Message types sent from device.
	/// </summary>
	public enum ReceiveMessageTypes:Byte {
		Hello = 0,
		TimeRequest = 1,
		Records = 4,
		CommitRequest = 5,
		VersionData = 14,
		AsyncMessageResponse = 21,
		RequestAsync = 22
	}
}