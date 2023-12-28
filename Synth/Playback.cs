using WINMM;

namespace Synth {
	internal class Playback : WaveOut {
		static Playback mInstance = null;
		static Sampler[] mSamplers = null;
		static Channel[] mChannels = null;
		static double[] mBufferL = null;
		static double[] mBufferR = null;

		public static void Setup(int sampleRate, int bufferLength) {
			if (null == mInstance) {
				mInstance = new Playback();
				mInstance.Open();
			}
			SystemValue.BufferLength = bufferLength;
			SystemValue.SampleRate = sampleRate;
			SystemValue.DeltaTime = 1.0 / sampleRate;
			mSamplers = Sampler.Construct();
			mChannels = Channel.Construct();
			mBufferL = new double[SystemValue.BufferLength];
			mBufferR = new double[SystemValue.BufferLength];
		}

		public static void Purge() {
			mInstance.Dispose();
		}

		public static void SendMessage(int port, byte[] message) {
			Channel.SendMessage(mChannels, mSamplers, port, message);
		}

		Playback() : base(SystemValue.SampleRate, 2, SystemValue.BufferLength * 2, 64) { }

		protected override void WriteBuffer() {
			Sampler.WriteBuffer(mSamplers);
			Channel.WriteBuffer(mChannels, mBufferL, mBufferR);
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
