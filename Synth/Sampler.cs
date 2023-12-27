using System;

namespace Synth {
	internal class Sampler {
		enum State {
			Free,
			Purge,
			Press,
			Release,
			Hold
		}

		class OSC {
			public double Phase;
			public double Value;
		}
		class LFO {
			public double Depth;
			public double Phase;
			public double Value;
		}
		class LPF {
			public double a11;
			public double a12;
			public double a21;
			public double a22;
			public double b11;
			public double b12;
			public double b21;
			public double b22;
		}

		delegate void WriteProc();

		const double ADJUST = 0.975 * 2 * Math.PI;
		const double INV_FACT2 = 5.00000000e-01;
		const double INV_FACT3 = 1.66666667e-01;
		const double INV_FACT4 = 4.16666667e-02;
		const double INV_FACT5 = 8.33333333e-03;
		const double INV_FACT6 = 1.38888889e-03;
		const double INV_FACT7 = 1.98412698e-04;
		const double INV_FACT8 = 2.48015873e-05;
		const double INV_FACT9 = 2.75573192e-06;

		const int SIN_LENGTH = 96;
		static double[] SIN_TABLE = null;
		static Sampler[] INSTANCES = null;

		WriteProc mWriteProc;
		State mState = State.Free;
		Channel mCh = null;
		int mNoteNum = 0;
		double mVelo = 0.0;
		double mGain = 0.0;
		double mPan = 0.0;
		double mDelta = 0.0;
		double mEgAmp = 0.0;
		double mEgLpf = 1.0;
		double mEgPitch = 1.0;
		bool mAmpAttack = true;
		bool mLpfAttack = true;
		bool mPitchAttack = true;
		OSC[] mOsc = new OSC[] {
			new OSC(), new OSC(),
			new OSC(), new OSC(),
			new OSC(), new OSC(),
			new OSC(), new OSC()
		};
		LFO[] mLfo = new LFO[] {
			new LFO(), new LFO()
		};
		LPF mLpfL = new LPF();
		LPF mLpfR = new LPF();

		public static void Construct() {
			if (null == INSTANCES) {
				INSTANCES = new Sampler[128];
				for (int i = 0; i < INSTANCES.Length; i++) {
					INSTANCES[i] = new Sampler();
				}
				SIN_TABLE = new double[SIN_LENGTH + 1];
				for (int i = 0; i < SIN_LENGTH; i++) {
					SIN_TABLE[i] = Math.Sin(2 * Math.PI * i / SIN_LENGTH);
				}
			}
		}
		public static void Purge(Channel ch) {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (smpl.mCh == ch && State.Press <= smpl.mState) {
					smpl.mState = State.Purge;
				}
			}
		}
		public static void HoldOff(Channel ch) {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (smpl.mCh == ch && smpl.mState == State.Hold) {
					smpl.mState = State.Release;
				}
			}
		}
		public static void NoteOff(Channel ch, int noteNum) {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (smpl.mCh == ch && smpl.mNoteNum == noteNum && smpl.mState == State.Press) {
					smpl.mState = ch.Hold ? State.Hold : State.Release;
				}
			}
		}
		public static void NoteOn(Channel ch, int noteNum, int velo) {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (smpl.mCh == ch && smpl.mNoteNum == noteNum && smpl.mState >= State.Press) {
					smpl.mState = State.Purge;
				}
			}
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (smpl.mState == State.Free) {
					smpl.mCh = ch;
					smpl.mNoteNum = noteNum;
					smpl.mVelo = velo / 127.0;
					smpl.mGain = 0.0;
					smpl.mPan = ch.Pan;
					smpl.mDelta = Math.Pow(2.0, (noteNum + 3) / 12.0) * 13.75 * 0.125 * SystemValue.DeltaTime;
					smpl.mLfo[0].Depth = 0.0;
					smpl.mLfo[0].Phase = 0.0;
					smpl.mLfo[0].Value = 0.0;
					smpl.mLfo[1].Depth = 0.0;
					smpl.mLfo[1].Phase = 0.0;
					smpl.mLfo[1].Value = 0.0;
					smpl.mEgAmp = 0.0;
					smpl.mEgLpf = ch.EG.LPF.Rise;
					smpl.mEgPitch = ch.EG.Pitch.Rise;
					smpl.mAmpAttack = true;
					smpl.mLpfAttack = true;
					smpl.mPitchAttack = true;
					smpl.mLpfL = new LPF();
					smpl.mLpfR = new LPF();
					smpl.mWriteProc = smpl.WriteSaw;
					smpl.mState = State.Press;
					return;
				}
			}
		}
		public static void WriteBuffer() {
			for (int i = 0; i < INSTANCES.Length; i++) {
				var smpl = INSTANCES[i];
				if (State.Purge <= smpl.mState) {
					smpl.mWriteProc();
				}
			}
		}

		Sampler() {
			mWriteProc = WriteNone;
		}

		void WriteNone() {
			mState = State.Free;
		}

		void WritePWM() {
			for (int i = 0; i < SystemValue.BufferLength; i++) {
				#region EG
				switch (mState) {
				case State.Purge:
					mEgAmp -= mEgAmp * 0.1;
					break;
				case State.Press:
					if (mAmpAttack) {
						mEgAmp += (1.0 - mEgAmp) * SystemValue.DeltaTime / mCh.EG.Amp.Attack;
						if (0.995 <= mEgAmp) {
							mAmpAttack = false;
						}
					} else {
						mEgAmp += (mCh.EG.Amp.Sustain - mEgAmp) * SystemValue.DeltaTime / mCh.EG.Amp.Decay;
					}
					if (mLpfAttack) {
						mEgLpf += (mCh.EG.LPF.Level - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Attack;
						if (0.995 * mCh.EG.LPF.Level <= mEgLpf && mEgLpf <= 1.005 * mCh.EG.LPF.Level) {
							mLpfAttack = false;
						}
					} else {
						mEgLpf += (mCh.EG.LPF.Sustain - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Decay;
					}
					if (mPitchAttack) {
						mEgPitch += (mCh.EG.Pitch.Level - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Attack;
						if (0.995 * mCh.EG.Pitch.Level <= mEgPitch && mEgPitch <= 1.005 * mCh.EG.Pitch.Level) {
							mPitchAttack = false;
						}
					} else {
						mEgPitch += (1.0 - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Decay;
					}
					break;
				case State.Release:
					mEgAmp -= mEgAmp * SystemValue.DeltaTime / mCh.EG.Amp.Release;
					mEgLpf += (mCh.EG.LPF.Fall - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Release;
					mEgPitch += (mCh.EG.Pitch.Fall - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Release;
					break;
				case State.Hold:
					mEgAmp -= mEgAmp * SystemValue.DeltaTime / mCh.EG.Amp.Hold;
					mEgLpf += (mCh.EG.LPF.Fall - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Release;
					mEgPitch += (mCh.EG.Pitch.Fall - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Release;
					break;
				}
				if (!(mState == State.Press && mAmpAttack) && mEgAmp < 0.001) {
					mState = State.Free;
					break;
				}
				mGain += (mCh.Gain * mEgAmp * mVelo - mGain) * 0.1;
				mPan += (mCh.Pan - mPan) * 0.1;
				#endregion
				#region LFO
				{
					var lfo = mLfo[0];
					lfo.Phase -= (int)lfo.Phase;
					var indexD = SIN_LENGTH * lfo.Phase;
					var index = (int)indexD;
					var a2b = indexD - index;
					lfo.Value = SIN_TABLE[index] * (1.0 - a2b) + SIN_TABLE[index + 1] * a2b;
					lfo.Value = 1.0 + lfo.Value * lfo.Depth;
					lfo.Depth += (mCh.LFO1.Depth - lfo.Depth) * SystemValue.DeltaTime / mCh.LFO1.Delay;
					lfo.Phase += mCh.LFO1.Rate * SystemValue.DeltaTime * 2;
				}
				{
					var lfo = mLfo[1];
					lfo.Phase -= (int)lfo.Phase;
					var indexD = SIN_LENGTH * lfo.Phase;
					var index = (int)indexD;
					var a2b = indexD - index;
					lfo.Value = SIN_TABLE[index] * (1.0 - a2b) + SIN_TABLE[index + 1] * a2b;
					lfo.Value *= lfo.Depth;
					lfo.Depth += (mCh.LFO2.Depth - lfo.Depth) * SystemValue.DeltaTime / mCh.LFO2.Delay;
					lfo.Phase += mCh.LFO2.Rate * SystemValue.DeltaTime * 2;
				}
				#endregion
				#region OSC
				var delta = mCh.Pitch * mEgPitch * mLfo[0].Value * mDelta;
				var sumL = 0.0;
				var sumR = 0.0;
				for (int o = 0; o < 8; o++) {
					var chOSC = mCh.OSC[o];
					var oGain = chOSC.Gain * mGain;
					var oDelta = chOSC.Pitch * delta;
					var oWidth = chOSC.Param + mLfo[1].Value;
					var oPan = Math.Max(-1, Math.Min(1, chOSC.Pan + mPan));
					var osc = mOsc[o];
					osc.Phase -= (int)osc.Phase;
					osc.Value = (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += (osc.Phase < oWidth) ? 0.125 : -0.125;
					osc.Phase += oDelta;
					osc.Value *= oGain;
					sumL += osc.Value;// * oPan;
					sumR += osc.Value;// * oPan;
				}
				#endregion
				#region LPF
				double ka1, ka2, kb1, kb2;
				{
					var rad = mEgLpf * ADJUST;
					var rad_2 = rad * rad;
					var c = INV_FACT8;
					c *= rad_2;
					c -= INV_FACT6;
					c *= rad_2;
					c += INV_FACT4;
					c *= rad_2;
					c -= INV_FACT2;
					c *= rad_2;
					c++;
					var s = INV_FACT9;
					s *= rad_2;
					s -= INV_FACT7;
					s *= rad_2;
					s += INV_FACT5;
					s *= rad_2;
					s -= INV_FACT3;
					s *= rad_2;
					s++;
					s *= rad;
					var alpha = s / (mCh.EG.LPF.Resonance * 4.0 + 1.0);
					var ka0 = alpha + 1.0;
					ka1 = -2.0 * c / ka0;
					ka2 = (1.0 - alpha) / ka0;
					kb1 = (1.0 - c) / ka0;
					kb2 = kb1 * 0.5;
				}
				{
					var input = sumL;
					var output = kb2 * input
						+ kb1 * mLpfL.b11
						+ kb2 * mLpfL.b12
						- ka1 * mLpfL.a11
						- ka2 * mLpfL.a12
					;
					mLpfL.a12 = mLpfL.a11;
					mLpfL.a11 = output;
					mLpfL.b12 = mLpfL.b11;
					mLpfL.b11 = input;
					input = output;
					output = kb2 * input
						+ kb1 * mLpfL.b21
						+ kb2 * mLpfL.b22
						- ka1 * mLpfL.a21
						- ka2 * mLpfL.a22
					;
					mLpfL.a22 = mLpfL.a21;
					mLpfL.a21 = output;
					mLpfL.b22 = mLpfL.b21;
					mLpfL.b21 = input;
					mCh.InputL[i] += output;
				}
				{
					var input = sumR;
					var output = kb2 * input
						+ kb1 * mLpfR.b11
						+ kb2 * mLpfR.b12
						- ka1 * mLpfR.a11
						- ka2 * mLpfR.a12
					;
					mLpfR.a12 = mLpfR.a11;
					mLpfR.a11 = output;
					mLpfR.b12 = mLpfR.b11;
					mLpfR.b11 = input;
					input = output;
					output = kb2 * input
						+ kb1 * mLpfR.b21
						+ kb2 * mLpfR.b22
						- ka1 * mLpfR.a21
						- ka2 * mLpfR.a22
					;
					mLpfR.a22 = mLpfR.a21;
					mLpfR.a21 = output;
					mLpfR.b22 = mLpfR.b21;
					mLpfR.b21 = input;
					mCh.InputR[i] += output;
				}
				#endregion
			}
		}

		void WriteSaw() {
			for (int i = 0; i < SystemValue.BufferLength; i++) {
				#region EG
				switch (mState) {
				case State.Purge:
					mEgAmp -= mEgAmp * 0.1;
					break;
				case State.Press:
					if (mAmpAttack) {
						mEgAmp += (1.0 - mEgAmp) * SystemValue.DeltaTime / mCh.EG.Amp.Attack;
						if (0.995 <= mEgAmp) {
							mAmpAttack = false;
						}
					} else {
						mEgAmp += (mCh.EG.Amp.Sustain - mEgAmp) * SystemValue.DeltaTime / mCh.EG.Amp.Decay;
					}
					if (mLpfAttack) {
						mEgLpf += (mCh.EG.LPF.Level - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Attack;
						if (0.995 * mCh.EG.LPF.Level <= mEgLpf && mEgLpf <= 1.005 * mCh.EG.LPF.Level) {
							mLpfAttack = false;
						}
					} else {
						mEgLpf += (mCh.EG.LPF.Sustain - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Decay;
					}
					if (mPitchAttack) {
						mEgPitch += (mCh.EG.Pitch.Level - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Attack;
						if (0.995 * mCh.EG.Pitch.Level <= mEgPitch && mEgPitch <= 1.005 * mCh.EG.Pitch.Level) {
							mPitchAttack = false;
						}
					} else {
						mEgPitch += (1.0 - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Decay;
					}
					break;
				case State.Release:
					mEgAmp -= mEgAmp * SystemValue.DeltaTime / mCh.EG.Amp.Release;
					mEgLpf += (mCh.EG.LPF.Fall - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Release;
					mEgPitch += (mCh.EG.Pitch.Fall - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Release;
					break;
				case State.Hold:
					mEgAmp -= mEgAmp * SystemValue.DeltaTime / mCh.EG.Amp.Hold;
					mEgLpf += (mCh.EG.LPF.Fall - mEgLpf) * SystemValue.DeltaTime / mCh.EG.LPF.Release;
					mEgPitch += (mCh.EG.Pitch.Fall - mEgPitch) * SystemValue.DeltaTime / mCh.EG.Pitch.Release;
					break;
				}
				if (!(mState == State.Press && mAmpAttack) && mEgAmp < 0.001) {
					mState = State.Free;
					break;
				}
				mGain += (mCh.Gain * mEgAmp * mVelo - mGain) * 0.1;
				mPan += (mCh.Pan - mPan) * 0.1;
				#endregion
				#region LFO
				{
					var lfo = mLfo[0];
					lfo.Phase -= (int)lfo.Phase;
					var indexD = SIN_LENGTH * lfo.Phase;
					var index = (int)indexD;
					var a2b = indexD - index;
					lfo.Value = SIN_TABLE[index] * (1.0 - a2b) + SIN_TABLE[index + 1] * a2b;
					lfo.Value = 1.0 + lfo.Value * lfo.Depth;
					lfo.Depth += (mCh.LFO1.Depth - lfo.Depth) * SystemValue.DeltaTime / mCh.LFO1.Delay;
					lfo.Phase += mCh.LFO1.Rate * SystemValue.DeltaTime * 2;
				}
				#endregion
				#region OSC
				var delta = mCh.Pitch * mEgPitch * mLfo[0].Value * mDelta;
				var sumL = 0.0;
				var sumR = 0.0;
				for (int o = 0; o < 8; o++) {
					var chOSC = mCh.OSC[o];
					var oGain = chOSC.Gain * mGain * 0.25;
					var oDelta = chOSC.Pitch * delta;
					var oPan = Math.Max(-1, Math.Min(1, chOSC.Pan + mPan));
					var osc = mOsc[o];
					osc.Phase -= (int)osc.Phase;
					osc.Value = osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Phase -= (int)osc.Phase;
					osc.Value += osc.Phase - (int)(osc.Phase * 2);
					osc.Phase += oDelta;
					osc.Value *= oGain;
					sumL += osc.Value;// * oPan;
					sumR += osc.Value;// * oPan;
				}
				#endregion
				#region LPF
				double ka1, ka2, kb1, kb2;
				{
					var rad = mEgLpf * ADJUST;
					var rad_2 = rad * rad;
					var c = INV_FACT8;
					c *= rad_2;
					c -= INV_FACT6;
					c *= rad_2;
					c += INV_FACT4;
					c *= rad_2;
					c -= INV_FACT2;
					c *= rad_2;
					c++;
					var s = INV_FACT9;
					s *= rad_2;
					s -= INV_FACT7;
					s *= rad_2;
					s += INV_FACT5;
					s *= rad_2;
					s -= INV_FACT3;
					s *= rad_2;
					s++;
					s *= rad;
					var alpha = s / (mCh.EG.LPF.Resonance * 4.0 + 1.0);
					var ka0 = alpha + 1.0;
					ka1 = -2.0 * c / ka0;
					ka2 = (1.0 - alpha) / ka0;
					kb1 = (1.0 - c) / ka0;
					kb2 = kb1 * 0.5;
				}
				{
					var input = sumL;
					var output = kb2 * input
						+ kb1 * mLpfL.b11
						+ kb2 * mLpfL.b12
						- ka1 * mLpfL.a11
						- ka2 * mLpfL.a12
					;
					mLpfL.a12 = mLpfL.a11;
					mLpfL.a11 = output;
					mLpfL.b12 = mLpfL.b11;
					mLpfL.b11 = input;
					input = output;
					output = kb2 * input
						+ kb1 * mLpfL.b21
						+ kb2 * mLpfL.b22
						- ka1 * mLpfL.a21
						- ka2 * mLpfL.a22
					;
					mLpfL.a22 = mLpfL.a21;
					mLpfL.a21 = output;
					mLpfL.b22 = mLpfL.b21;
					mLpfL.b21 = input;
					mCh.InputL[i] += output;
				}
				{
					var input = sumR;
					var output = kb2 * input
						+ kb1 * mLpfR.b11
						+ kb2 * mLpfR.b12
						- ka1 * mLpfR.a11
						- ka2 * mLpfR.a12
					;
					mLpfR.a12 = mLpfR.a11;
					mLpfR.a11 = output;
					mLpfR.b12 = mLpfR.b11;
					mLpfR.b11 = input;
					input = output;
					output = kb2 * input
						+ kb1 * mLpfR.b21
						+ kb2 * mLpfR.b22
						- ka1 * mLpfR.a21
						- ka2 * mLpfR.a22
					;
					mLpfR.a22 = mLpfR.a21;
					mLpfR.a21 = output;
					mLpfR.b22 = mLpfR.b21;
					mLpfR.b21 = input;
					mCh.InputR[i] += output;
				}
				#endregion
			}
		}
	}
}
