namespace Synth {
	internal class SystemValue {
		public static int BufferLength = 256;
		public static int SampleRate = 44100;
		public static double DeltaTime = 1.0 / SampleRate;
		public static double[] BufferL = null;
		public static double[] BufferR = null;
	}
}
