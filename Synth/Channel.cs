using System;

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

		public Instruments.OSC[] OSC = Instruments.OSC.GetDefault(8);
		public Instruments.EG EG = Instruments.EG.GetDefault();
		public Instruments.LFO LFO1 = new Instruments.LFO(0.1);
		public Instruments.LFO LFO2 = new Instruments.LFO(0.0);

		public double Gain = 0.5;
		public double Pitch = 1.0;
		public double Pan = 0.0;
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
				ch.CtrlChg(message[1], message[2]);
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

		void CtrlChg(int type, int value) {

		}

		void ProgChg(int num) {
			OSC[0] = new Instruments.OSC(0.33, 0.975, 0, 0.5);
			OSC[1] = new Instruments.OSC(0.25, 0.98, 0, 0.5);
			OSC[2] = new Instruments.OSC(0.25, 0.99, 0, 0.5);
			OSC[3] = new Instruments.OSC(0.66, 1.00, 0, 0.5);
			OSC[4] = new Instruments.OSC(0.25, 1.01, 0, 0.5);
			OSC[5] = new Instruments.OSC(0.25, 1.02, 0, 0.5);
			OSC[6] = new Instruments.OSC(0.33, 1.025, 0, 0.5);
		}

		void PitchBend(int msb, int lsb) {

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
