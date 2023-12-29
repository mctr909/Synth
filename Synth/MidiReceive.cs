using WINMM;

namespace Synth {
	internal class MidiReceive : MidiIn {
		protected override void Receive(byte[] message) {
			Playback.SendMessage(0, message);
		}
	}
}
