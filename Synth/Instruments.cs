using System;
using System.Runtime.InteropServices;

namespace Synth {
	internal class Instruments {
		[StructLayout(LayoutKind.Sequential)]
		public struct EG_AMP {
			public double Attack;
			public double Decay;
			public double Release;
			public double Hold;
			public double Sustain;
			public static EG_AMP GetDefault() {
				return new EG_AMP() {
					Attack = 0.001,
					Decay = 0.1,
					Release = 0.01,
					Hold = 1.0,
					Sustain = 0.5
				};
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct EG_LPF {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Sustain;
			public double Fall;
			public double Resonance;
			public static EG_LPF GetDefault() {
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
		[StructLayout(LayoutKind.Sequential)]
		public struct EG_PITCH {
			public double Attack;
			public double Decay;
			public double Release;
			public double Rise;
			public double Level;
			public double Fall;
			public static EG_PITCH GetDefault() {
				return new EG_PITCH() {
					Attack = 0.02,
					Decay = 0.001,
					Release = 0.001,
					Rise = Math.Pow(2.0, -1.0 / 12.0),
					Level = 1.0,
					Fall = 1.0
				};
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct EG {
			public EG_AMP Amp;
			public EG_LPF LPF;
			public EG_PITCH Pitch;
			public static EG GetDefault() {
				return new EG() {
					Amp = EG_AMP.GetDefault(),
					LPF = EG_LPF.GetDefault(),
					Pitch = EG_PITCH.GetDefault()
				};
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct OSC {
			public double Gain;
			public double Pitch;
			public double Pan;
			public double Param;
			public OSC(double gain, double pitch = 1.0, double pan = 0.0, double param = 0.0) {
				Gain = gain;
				Pitch = pitch;
				Pan = pan;
				Param = param;
			}
			public static OSC[] GetDefault(int count) {
				var ret = new OSC[count];
				ret[0] = new OSC(0.25);
				return ret;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct LFO {
			public double Depth;
			public double Rate;
			public double Delay;
			public LFO(double depth, double rate = 4, double delay = 0.2) {
				Depth = depth;
				Rate = rate;
				Delay = delay;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct WAVE_INFO {
			public int SampleRate;
			public int LoopBegin;
			public int LoopLength;
			public bool LoopEnable;
			public byte UnityNote;
			private ushort Reserved;
			public double Gain;
			public double Tune;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct REGION {
			public byte NoteLow;
			public byte NoteHigh;
			public byte VeloLow;
			public byte VeloHigh;
			public ushort Layer;
			public ushort WaveIndex;
			public double Gain;
			public double Pan;
			public double Pitch;
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct INST {
			public enum TYPE : byte {
				PCM_NOTE,
				PCM_DRUM,
				PWM,
				SAW,
				TRI,
				TRI_STEP,
				FM
			}
			public TYPE Type;
			public byte BankMSB;
			public byte BankLSB;
			public byte ProgNum;
			public int RegionBegin;
			public int RegionCount;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
			public string Name;
		}
	}
}
