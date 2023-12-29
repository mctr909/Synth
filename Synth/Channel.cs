using System;
using static Synth.Instruments;

namespace Synth {
	internal class Channel {
		enum State {
			Free,
			Standby,
			Active
		}

		public struct DELAY {
			public double Send;
			public double Feedback;
			public double Time;
			public double Cross;
			public DELAY(double send, double feedback = 0.5, double time = 0.2, double cross = 0.33) {
				Send = send;
				Feedback = feedback;
				Time = time;
				Cross = cross;
			}
		}

		public DELAY Delay = new DELAY(0.5);

		public OSC[] OSC = Instruments.OSC.GetDefault(8);
		public EG EG = EG.GetDefault();
		public LFO LFO1 = new LFO(0.0);
		public LFO LFO2 = new LFO(0.0);

		public INST Inst = INST.GetDefaoult();

		public double Gain = 0.5;
		public double Pitch = 1.0;
		public double Resonance = 0.0;
		public bool Hold = false;
		public double[] InputL = null;
		public double[] InputR = null;

		public double RmsL = 0.0;
		public double RmsR = 0.0;
		public double PeakL = 0.0;
		public double PeakR = 0.0;

		State mState = State.Free;
		double mRmsSumL = 0.0;
		double mRmsSumR = 0.0;
		double mDelayWritePos = 0.0;
		double[] mDelayTapL = null;
		double[] mDelayTapR = null;

		public byte Vol = 100;
		public byte Exp = 100;
		public byte Pan = 64;
		public byte DelaySend = 0;
		public byte ChorusSend = 0;
		public byte BendWidth = 2;
		short mPitch = 0;
		byte mRpnMSB = 127;
		byte mRpnLSB = 127;

		public static Channel[] Construct(int ports = 1) {
			var ret = new Channel[16 * ports];
			for (int i = 0; i < ret.Length; i++) {
				ret[i] = new Channel();
			}
			for (int i = 0; i < ret.Length; i++) {
				var ch = ret[i];
				ch.InputL = new double[SystemValue.BufferLength];
				ch.InputR = new double[SystemValue.BufferLength];
				ch.mDelayTapL = new double[SystemValue.SampleRate];
				ch.mDelayTapR = new double[SystemValue.SampleRate];
				ch.mDelayWritePos = 0.0;
			}
			return ret;
		}

		public static void WriteBuffer(Channel[] channels, double[] bufferL, double[] bufferR) {
			for (int i = 0; i < channels.Length; i++) {
				var ch = channels[i];
				if (State.Standby <= ch.mState) {
					ch.Write(bufferL, bufferR);
				}
			}
		}

		public static void SendMessage(Channel[] channels, Sampler[] samplers, int port, byte[] message) {
			var type = message[0] & 0xF0;
			Channel ch;
			if (0x80 <= type && type <= 0xE0) {
				if (9 == (message[0] & 0xF)) {
					return;
				}
				var chNum = (port << 4) | message[0] & 0xF;
				if (channels.Length <= chNum) {
					return;
				}
				ch = channels[chNum];
			} else {
				return;
			}
			switch (type) {
			case 0x80:
				Sampler.NoteOff(samplers, ch, message[1]);
				break;
			case 0x90:
				if (0 == message[2]) {
					Sampler.NoteOff(samplers, ch, message[1]);
				} else {
					if (ch.mState == State.Free) {
						ch.mState = State.Standby;
					}
					Sampler.NoteOn(samplers, ch, message[1], message[2]);
				}
				break;
			case 0xA0:
				break;
			case 0xB0:
				switch (message[1]) {
				case 64:
					ch.Hold = 64 <= message[2];
					if (!ch.Hold) {
						Sampler.HoldOff(samplers, ch);
					}
					break;
				case 120:
				case 123:
					Sampler.Purge(samplers, ch);
					break;
				default:
					ch.CtrlChg(message[1], message[2]);
					break;
				}
				break;
			case 0xC0:
				ch.ProgChg(message[1]);
				break;
			case 0xD0:
				break;
			case 0xE0:
				ch.PitchBend(message[1], message[2]);
				break;
			}
		}

		Channel() { }

		void CtrlChg(byte type, byte value) {
			switch (type) {
			case 6:
				if (mRpnMSB == 0 && mRpnLSB == 0) {
					BendWidth = value;
					mRpnMSB = 127;
					mRpnLSB = 127;
				}
				break;
			case 7:
				Vol = value;
				Gain = Vol * Vol * Exp * Exp / 16129.0 / 16129.0 * 0.5;
				break;
			case 10:
				Pan = value;
				break;
			case 11:
				Exp = value;
				Gain = Vol * Vol * Exp * Exp / 16129.0 / 16129.0 * 0.5;
				break;
			case 94:
				DelaySend = value;
				Delay.Feedback = value * 0.8 / 127.0;
				Delay.Send = value / 127.0;
				break;
			case 100:
				mRpnLSB = value;
				break;
			case 101:
				mRpnMSB = value;
				break;
			case 121:
				Pan = 64;
				Vol = 100;
				Exp = 100;
				Gain = Vol * Vol * Exp * Exp / 16129.0 / 16129.0 * 0.5;
				Hold = false;
				mPitch = 0;
				Pitch = 1.0;
				break;
			}
		}

		void ProgChg(byte num) {
			if (32 <= num && num <= 39) {
				if (36 == num || 37 == num || 39 == num) {
					Inst.Type = INST.TYPE.PWM;
				} else {
					Inst.Type = INST.TYPE.SAW;
				}
				EG = EG.GetDefault();
				EG.Amp.Decay = 0.5;
				EG.Amp.Sustain = 0.8;
				EG.Amp.Release = 0.005;
				EG.Pitch.Rise = 1.0;
				EG.LPF.Attack = 0.001;
				EG.LPF.Decay = 0.04;
				EG.LPF.Rise = 4000 / 44100.0;
				EG.LPF.Level = 4000 / 44100.0;
				EG.LPF.Sustain = 120 / 44100.0;
				EG.LPF.Resonance = 0.5;
				LFO1.Depth = 0.0;
				Delay.Feedback = DelaySend / 127.0;
				Delay.Send = DelaySend / 127.0;
				OSC[0] = new OSC(0.66);
				OSC[1] = new OSC(0.0);
				OSC[2] = new OSC(0.0);
				OSC[3] = new OSC(0.0);
				OSC[4] = new OSC(0.0);
				OSC[5] = new OSC(0.0);
				OSC[6] = new OSC(0.0);
			} else if (40 <= num && num <= 55) {
				Inst.Type = INST.TYPE.SAW;
				EG = EG.GetDefault();
				EG.Amp.Decay = 0.5;
				EG.Amp.Sustain = 0.8;
				EG.Amp.Release = 0.1;
				EG.LPF.Level = 8000 / 44100.0;
				EG.LPF.Resonance = 0.0;
				EG.Pitch.Rise = 1.0;
				LFO1.Depth = 0.0;
				Delay.Feedback = DelaySend / 127.0;
				Delay.Send = DelaySend / 127.0;
				OSC[0] = new OSC(0.30, Math.Pow(2, -0.3 / 12.0), -24, 0.5);
				OSC[1] = new OSC(0.20, Math.Pow(2, -0.2 / 12.0), 12, 0.5);
				OSC[2] = new OSC(0.20, Math.Pow(2, -0.1 / 12.0), -6, 0.5);
				OSC[3] = new OSC(0.50, 1.00, 0, 0.5);
				OSC[4] = new OSC(0.20, Math.Pow(2, 0.1 / 12.0), 6, 0.5);
				OSC[5] = new OSC(0.20, Math.Pow(2, 0.2 / 12.0), -12, 0.5);
				OSC[6] = new OSC(0.30, Math.Pow(2, 0.3 / 12.0), 24, 0.5);
			} else if ((56 <= num && num <= 63) || 81 == num || (88 <= num && num <= 103)) {
				Inst.Type = INST.TYPE.SAW;
				EG = EG.GetDefault();
				EG.Amp.Decay = 0.5;
				EG.Amp.Sustain = 0.8;
				EG.Amp.Release = 0.05;
				EG.Pitch.Rise = Math.Pow(2, 9.0 / 12.0);
				EG.Pitch.Attack = 0.008;
				LFO1.Delay = 0.33;
				LFO1.Depth = Math.Pow(2, 0.3 / 12.0) - 1.0;
				Delay.Feedback = DelaySend / 127.0;
				Delay.Send = DelaySend / 127.0;
				OSC[0] = new OSC(0.30, Math.Pow(2, -0.3 / 12.0), -24, 0.5);
				OSC[1] = new OSC(0.20, Math.Pow(2, -0.2 / 12.0), 12, 0.5);
				OSC[2] = new OSC(0.20, Math.Pow(2, -0.1 / 12.0), -6, 0.5);
				OSC[3] = new OSC(0.50, 1.00, 0, 0.5);
				OSC[4] = new OSC(0.20, Math.Pow(2, 0.1 / 12.0), 6, 0.5);
				OSC[5] = new OSC(0.20, Math.Pow(2, 0.2 / 12.0), -12, 0.5);
				OSC[6] = new OSC(0.30, Math.Pow(2, 0.3 / 12.0), 24, 0.5);
			} else {
				if (80 == num) {
					Inst.Type = INST.TYPE.PWM;
				} else {
					Inst.Type = INST.TYPE.SAW;
				}
				EG = EG.GetDefault();
				EG.Amp.Decay = 0.4;
				EG.Amp.Sustain = 0.0;
				EG.Amp.Release = 0.005;
				EG.LPF.Attack = 0.001;
				EG.LPF.Decay = 0.1;
				EG.LPF.Rise = 10000 / 44100.0;
				EG.LPF.Level = 10000 / 44100.0;
				EG.LPF.Sustain = 800 / 44100.0;
				EG.Pitch.Rise = 1.0;
				LFO1.Depth = 0.0;
				Delay.Feedback = DelaySend / 127.0;
				Delay.Send = DelaySend / 127.0;
				OSC[0] = new OSC(0.66);
				OSC[1] = new OSC(0.0);
				OSC[2] = new OSC(0.0);
				OSC[3] = new OSC(0.0);
				OSC[4] = new OSC(0.0);
				OSC[5] = new OSC(0.0);
				OSC[6] = new OSC(0.0);
			}
		}

		void PitchBend(byte lsb, byte msb) {
			mPitch = (short)(((msb << 7) | lsb) - 8192);
			var v = mPitch * BendWidth / 8192.0;
			Pitch = Math.Pow(2.0, v / 12.0);
		}

		void Write(double[] bufferL, double[] bufferR) {
			for (int i = 0; i < SystemValue.BufferLength; i++) {
				var outputL = InputL[i];
				var outputR = InputR[i];
				InputL[i] = 0.0;
				InputR[i] = 0.0;

				#region delay
				{
					var readPos = mDelayWritePos - Delay.Time + 1.0;
					readPos -= (int)readPos;
					var readIndex = (int)(readPos * SystemValue.SampleRate);
					var tempL = mDelayTapL[readIndex];
					var tempR = mDelayTapR[readIndex];
					var delayL = tempL * (1.0 - Delay.Cross) + tempR * Delay.Cross;
					var delayR = tempR * (1.0 - Delay.Cross) + tempL * Delay.Cross;
					mDelayWritePos -= (int)mDelayWritePos;
					var writeIndex = (int)(mDelayWritePos * SystemValue.SampleRate);
					mDelayWritePos += SystemValue.DeltaTime;
					mDelayTapL[writeIndex] = outputL + delayL * Delay.Feedback;
					mDelayTapR[writeIndex] = outputR + delayR * Delay.Feedback;
					outputL += delayL * Delay.Send;
					outputR += delayR * Delay.Send;
				}
				#endregion

				#region meter
				{
					mRmsSumL += outputL * outputL;
					mRmsSumR += outputR * outputR;
					var att = 1.0 - 3.0 / SystemValue.SampleRate;
					mRmsSumL *= att;
					mRmsSumR *= att;
					var gain = 1.0 / att - 1.0;
					RmsL = mRmsSumL * gain;
					RmsR = mRmsSumR * gain;
					att = 1.0 - 10.0 / SystemValue.SampleRate;
					PeakL = Math.Max(PeakL * att, Math.Abs(outputL));
					PeakR = Math.Max(PeakR * att, Math.Abs(outputR));
				}
				#endregion

				if (mState == State.Active) {
					if (RmsL < 0.0000001 && RmsR < 0.0000001) {
						mState = State.Free;
						break;
					}
				}
				if (mState == State.Standby) {
					if (0.0001 <= RmsL || 0.0001 <= RmsR) {
						mState = State.Active;
					}
				}
				bufferL[i] += outputL;
				bufferR[i] += outputR;
			}
		}
	}
}
