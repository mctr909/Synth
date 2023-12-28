using WINMM;

namespace Synth {
	internal class Playback : WaveOut {
		double[] mBufferL = null;
		double[] mBufferR = null;

		public Playback(int sampleRate, int bufferLength) : base(sampleRate, 2, bufferLength * 2, 64) {
			SystemValue.BufferLength = bufferLength;
			SystemValue.SampleRate = sampleRate;
			SystemValue.DeltaTime = 1.0 / sampleRate;
			Sampler.Construct();
			Channel.Construct();
			mBufferL = new double[bufferLength];
			mBufferR = new double[bufferLength];
		}

		protected override void WriteBuffer() {
			Sampler.WriteBuffer();
			Channel.WriteBuffer(mBufferL, mBufferR);
			for (int i = 0, t = 0; i < BufferSize; i += 2, t++) {
				var tempL = mBufferL[t];
				var tempR = mBufferR[t];
				mBufferL[t] = 0.0;
				mBufferR[t] = 0.0;
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
