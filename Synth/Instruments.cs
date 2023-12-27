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
					Decay = 0.1,
					Release = 0.1,
					Hold = 0.001,
					Sustain = 0.66
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
					Attack = 0.05,
					Decay = 0.1,
					Release = 0.1,
					Rise = 12000 / 44100.0,
					Level = 4000 / 44100.0,
					Sustain = 8000 / 44100.0,
					Fall = 2000 / 44100.0,
					Resonance = 0.1
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
					Attack = 0.02,
					Decay = 0.001,
					Release = 0.001,
					Rise = 0.9,
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
			public static LFO Construct(double depth = 0.0, double rate = 4) {
				return new LFO() {
					Delay = 0.5,
					Depth = depth,
					Rate = rate
				};
			}
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
			public static DELAY Construct() {
				return new DELAY() {
					Time = 0.2,
					Send = 0.4,
					Cross = 0.33
				};
			}
		}
	}
}