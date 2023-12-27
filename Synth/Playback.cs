namespace Synth {
	internal class Playback : WaveOut {
		public Playback(int sampleRate, int bufferLength) : base(sampleRate, 2, bufferLength * 2, 64) {
			SystemValue.SetupBuffer(SampleRate, bufferLength);
			Sampler.Construct();
			Channel.Construct();
		}

		protected override void WriteBuffer() {
			Sampler.WriteBuffer();
			Channel.WriteBuffer();
			for (int i = 0, t = 0; i < BufferSize; i += 2, t++) {
				var tempL = SystemValue.BufferL[t];
				var tempR = SystemValue.BufferR[t];
				SystemValue.BufferL[t] = 0.0;
				SystemValue.BufferR[t] = 0.0;
				if (tempL < -1.0) {
					tempL = -1.0;
				}
				if (1.0 < tempL) {
					tempL = 1.0;
				}
				if (tempR < -1.0) {
					tempR = -1.0;
				}
				if (1.0 < tempR) {
					tempR = 1.0;
				}
				mBuffer[i] = (short)(tempL * 32767);
				mBuffer[i + 1] = (short)(tempR * 32767);
			}
		}
	}
}
