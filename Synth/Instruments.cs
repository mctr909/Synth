namespace Synth {
	internal class Instruments {
		public struct EG_AMP {
			public double Attack;
			public double Decay;
			public double Release;
			public double Hold;
			public double Sustain;
		}
		public struct EG_LPF {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Sustain;
			public double Fall;
		}
		public struct EG_PITCH {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Fall;
		}
		public struct EG {
			public EG_AMP Amp;
			public EG_LPF LPF;
			public EG_PITCH Pitch;
		}
		public struct LFO {
			public double Delay;
			public double Depth;
			public double Rate;
		}
		public struct OSC {
			public double Gain;
			public double Pitch;
			public double Param;
			public int Pan;
		}
		public struct DELAY {
			public double Time;
			public double Send;
			public double Cross;
		}
	}
}