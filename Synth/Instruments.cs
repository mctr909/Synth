namespace Synth {
	internal class Instruments {
		public struct EG_AMP {
			public double Attack;
			public double Decay;
			public double Release;
			public double Hold;
			public double Sustain;
			public static EG_AMP Construct() {
				return new EG_AMP() {
					Attack = 0.001,
					Decay = 0.001,
					Release = 0.01,
					Hold = 0.001,
					Sustain = 1.0
				};
			}
		}
		public struct EG_LPF {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Sustain;
			public double Fall;
			public double Resonance;
			public static EG_LPF Construct() {
				return new EG_LPF() {
					Attack = 0.001,
					Decay = 0.05,
					Release = 0.001,
					Rise = 6000 / 44100.0,
					Level = 6000 / 44100.0,
					Sustain = 80 / 44100.0,
					Fall = 80 / 44100.0,
					Resonance = 0.4
				};
			}
		}
		public struct EG_PITCH {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Fall;
			public static EG_PITCH Construct() {
				return new EG_PITCH() {
					Attack = 0.001,
					Decay = 0.001,
					Release = 0.001,
					Rise = 1.0,
					Level = 1.0,
					Fall = 1.0
				};
			}
		}
		public struct EG {
			public EG_AMP Amp;
			public EG_LPF LPF;
			public EG_PITCH Pitch;
			public static EG Construct() {
				return new EG() {
					Amp = EG_AMP.Construct(),
					LPF = EG_LPF.Construct(),
					Pitch = EG_PITCH.Construct()
				};
			}
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