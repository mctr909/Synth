using System;

namespace Synth {
	internal class Channel {
		public enum State {
			Free,
			Standby,
			Active
		}

		public Instruments.EG EG = Instruments.EG.Construct();
		public Instruments.LFO LFO1 = new Instruments.LFO();
		public Instruments.LFO LFO2 = new Instruments.LFO();
		public Instruments.OSC[] OSC = new Instruments.OSC[8];
		public Instruments.DELAY Delay = new Instruments.DELAY();

		public double Gain = 1.0;
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

		static Channel[] INSTANCES = null;

		public static void Construct(int ports = 1) {
			if (null == INSTANCES) {
				INSTANCES = new Channel[16 * ports];
				for (int i = 0; i < INSTANCES.Length; i++) {
					var ch = new Channel();
					ch.OSC[0].Gain = 1.0;
					ch.OSC[0].Pitch = 1.0;
					ch.OSC[0].Param = 0.5;
					INSTANCES[i] = ch;
				}
			}
			for (int i = 0; i < INSTANCES.Length; i++) {
				var ch = INSTANCES[i];
				ch.InputL = new double[SystemValue.BufferLength];
				ch.InputR = new double[SystemValue.BufferLength];
				ch.mDelayTapL = new double[SystemValue.SampleRate];
				ch.mDelayTapR = new double[SystemValue.SampleRate];
				ch.mDelayWritePos = 0.0;
			}
		}

		public static void WriteBuffer() {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var ch = INSTANCES[i];
				if (State.Standby <= ch.mState) {
					ch.Write();
				}
			}
		}

		public static void SendMessage(int port, byte[] message) {
			var type = message[0] & 0xF0;
			Channel ch;
			if (0x80 <= type && type <= 0xE0) {
				var chNum = (port << 4) | message[0] & 0xF;
				if (INSTANCES.Length <= chNum) {
					return;
				}
				ch = INSTANCES[chNum];
			} else {
				return;
			}
			switch (type) {
			case 0x80:
				Sampler.NoteOff(ch, message[1]);
				break;
			case 0x90:
				if (0 == message[2]) {
					Sampler.NoteOff(ch, message[1]);
				} else {
					if (ch.mState == State.Free) {
						ch.mState = State.Standby;
					}
					Sampler.NoteOn(ch, message[1], message[2]);
				}
				break;
			case 0xA0:
				break;
			case 0xB0: // Control change
				break;
			case 0xC0: // Program change
				break;
			case 0xD0:
				break;
			case 0xE0: // Pitch bend;
				break;
			}
		}

		void Write() {
			for (int i = 0; i < SystemValue.BufferLength; i++) {
				var outputL = InputL[i];
				var outputR = InputR[i];
				InputL[i] = 0.0;
				InputR[i] = 0.0;

				#region delay
				{
					var delayReadPos = mDelayWritePos - Delay.Time + 1.0;
					delayReadPos -= (int)delayReadPos;
					var readIndex = (int)(delayReadPos * SystemValue.SampleRate);
					var delayL = mDelayTapL[readIndex];
					var delayR = mDelayTapR[readIndex];
					var crossL = delayL * (1.0 - Delay.Cross) + delayR * Delay.Cross;
					var crossR = delayR * (1.0 - Delay.Cross) + delayL * Delay.Cross;
					outputL += crossL * Delay.Send;
					outputR += crossR * Delay.Send;
					mDelayWritePos -= (int)mDelayWritePos;
					var writeIndex = (int)(mDelayWritePos * SystemValue.SampleRate);
					mDelayWritePos += SystemValue.DeltaTime;
					mDelayTapL[writeIndex] = outputL;
					mDelayTapR[writeIndex] = outputR;
				}
				#endregion

				#region meter
				{
					mRmsSumL += outputL * outputL;
					mRmsSumR += outputR * outputR;
					var att = 1.0 - 0.2 / SystemValue.SampleRate;
					mRmsSumL *= att;
					mRmsSumR *= att;
					var gain = 1.0 / att - 1.0;
					RmsL = mRmsSumL * gain;
					RmsR = mRmsSumR * gain;
					att = 1.0 - 1.0 / SystemValue.SampleRate;
					PeakL = Math.Max(PeakL * att, Math.Abs(outputL));
					PeakR = Math.Max(PeakR * att, Math.Abs(outputR));
				}
				#endregion

				if (mState == State.Active) {
					if (RmsL < 0.000001 && RmsR < 0.000001) {
						mState = State.Free;
						break;
					}
				}
				if (mState == State.Standby) {
					if (0.00001 <= RmsL || 0.00001 <= RmsR) {
						mState = State.Active;
					}
				}

				SystemValue.BufferL[i] += outputL;
				SystemValue.BufferR[i] += outputR;
			}
		}
	}
}
