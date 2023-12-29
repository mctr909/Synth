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
					Sustain = 0.01
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
					Attack = 0.01,
					Decay = 0.001,
					Release = 0.001,
					Rise = Math.Pow(2.0, 1.0 / 12.0),
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
			public int Offset;
			public int SampleRate;
			public int LoopBegin;
			public int LoopLength;
			public bool LoopEnable;
			public byte UnityNote;
			private ushort Reserved;
			public double Gain;
			public double Tune;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 28)]
			public string Name;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct OSC {
			public byte NoteLow;
			public byte NoteHigh;
			public byte VeloLow;
			public byte VeloHigh;
			public int WaveIndex;
			public int Pan;
			public double Gain;
			public double Pitch;
			public double Param;
			public OSC(double gain, double pitch = 1.0, int pan = 0, double param = 0.5) {
				NoteLow = 0;
				NoteHigh = 127;
				VeloLow = 0;
				VeloHigh = 127;
				WaveIndex = 0;
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
			public int OscBegin;
			public int OscCount;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string Name;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
			public string Category;
			public static INST GetDefaoult() {
				return new INST() {
					Type = TYPE.SAW,
					Name = "Default",
					Category = ""
				};
			}
		}
	}
}
