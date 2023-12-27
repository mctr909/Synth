namespace Synth {
	internal class SystemValue {
		public static int BufferLength = 256;
		public static int SampleRate = 44100;
		public static double DeltaTime = 1.0 / SampleRate;
		public static double[] BufferL = null;
		public static double[] BufferR = null;
		public static void SetupBuffer(int sampleRate, int bufferLength) {
			BufferLength = bufferLength;
			SampleRate = sampleRate;
			DeltaTime = 1.0 / sampleRate;
			BufferL = new double[BufferLength];
			BufferR = new double[BufferLength];
		}
	}
}
