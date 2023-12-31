﻿using System;

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

		static readonly double[] PAN_L = new double[] {
			1.000,1.000,1.000,0.999,0.999,0.998,0.997,0.996,
			0.995,0.994,0.992,0.991,0.989,0.987,0.985,0.983,
			0.981,0.978,0.976,0.973,0.970,0.967,0.964,0.960,
			0.957,0.953,0.950,0.946,0.942,0.937,0.933,0.929,

			0.924,0.919,0.914,0.909,0.904,0.899,0.893,0.888,
			0.882,0.876,0.870,0.864,0.858,0.851,0.845,0.838,
			0.831,0.825,0.818,0.810,0.803,0.796,0.788,0.781,
			0.773,0.765,0.757,0.749,0.741,0.733,0.724,0.716,

			0.707,0.698,0.690,0.681,0.672,0.662,0.653,0.644,
			0.634,0.625,0.615,0.606,0.596,0.586,0.576,0.566,
			0.556,0.545,0.535,0.525,0.514,0.504,0.493,0.482,
			0.471,0.461,0.450,0.439,0.428,0.416,0.405,0.394,

			0.383,0.371,0.360,0.348,0.337,0.325,0.314,0.302,
			0.290,0.279,0.267,0.255,0.243,0.231,0.219,0.207,
			0.195,0.183,0.171,0.159,0.147,0.135,0.122,0.110,
			0.098,0.086,0.074,0.061,0.049,0.037,0.025,0.012
		};
		static readonly double[] PAN_R = new double[] {
			0.000,0.012,0.025,0.037,0.049,0.061,0.074,0.086,
			0.098,0.110,0.122,0.135,0.147,0.159,0.171,0.183,
			0.195,0.207,0.219,0.231,0.243,0.255,0.267,0.279,
			0.290,0.302,0.314,0.325,0.337,0.348,0.360,0.371,

			0.383,0.394,0.405,0.416,0.428,0.439,0.450,0.461,
			0.471,0.482,0.493,0.504,0.514,0.525,0.535,0.545,
			0.556,0.566,0.576,0.586,0.596,0.606,0.615,0.625,
			0.634,0.644,0.653,0.662,0.672,0.681,0.690,0.698,

			0.707,0.716,0.724,0.733,0.741,0.749,0.757,0.765,
			0.773,0.781,0.788,0.796,0.803,0.810,0.818,0.825,
			0.831,0.838,0.845,0.851,0.858,0.864,0.870,0.876,
			0.882,0.888,0.893,0.899,0.904,0.909,0.914,0.919,

			0.924,0.929,0.933,0.937,0.942,0.946,0.950,0.953,
			0.957,0.960,0.964,0.967,0.970,0.973,0.976,0.978,
			0.981,0.983,0.985,0.987,0.989,0.991,0.992,0.994,
			0.995,0.996,0.997,0.998,0.999,0.999,1.000,1.000
		};

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

		public static Sampler[] Construct() {
			var ret = new Sampler[128];
			var rnd = new Random();
			for (int i = 0; i < ret.Length; i++) {
				var smpl = new Sampler();
				for (int o = 0; o < smpl.mOsc.Length; o++) {
					smpl.mOsc[o].Phase = rnd.NextDouble();
				}
				ret[i] = smpl;
			}
			SIN_TABLE = new double[SIN_LENGTH + 1];
			for (int i = 0; i < SIN_LENGTH; i++) {
				SIN_TABLE[i] = Math.Sin(2 * Math.PI * i / SIN_LENGTH);
			}
			return ret;
		}
		public static void Purge(Sampler[] samplers, Channel ch) {
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
				if (smpl.mCh == ch && State.Press <= smpl.mState) {
					smpl.mState = State.Purge;
				}
			}
		}
		public static void HoldOff(Sampler[] samplers, Channel ch) {
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
				if (smpl.mCh == ch && smpl.mState == State.Hold) {
					smpl.mState = State.Release;
				}
			}
		}
		public static void NoteOff(Sampler[] samplers, Channel ch, int noteNum) {
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
				if (smpl.mCh == ch && smpl.mNoteNum == noteNum && smpl.mState == State.Press) {
					smpl.mState = ch.Hold ? State.Hold : State.Release;
				}
			}
		}
		public static void NoteOn(Sampler[] samplers, Channel ch, int noteNum, int velo) {
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
				if (smpl.mCh == ch && smpl.mNoteNum == noteNum && smpl.mState >= State.Press) {
					smpl.mState = State.Purge;
				}
			}
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
				if (smpl.mState == State.Free) {
					smpl.mCh = ch;
					smpl.mNoteNum = noteNum;
					smpl.mVelo = velo / 127.0;
					smpl.mGain = 0.0;
					smpl.mPan = ch.Pan;
					smpl.mDelta = Math.Pow(2.0, (noteNum - 9) / 12.0) * 13.75 * 0.125 * SystemValue.DeltaTime;
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
					switch (ch.Inst.Type) {
					case Instruments.INST.TYPE.PWM:
						smpl.mWriteProc = smpl.WritePWM;
						break;
					case Instruments.INST.TYPE.SAW:
						smpl.mWriteProc = smpl.WriteSaw;
						break;
					}
					smpl.mState = State.Press;
					return;
				}
			}
		}
		public static void WriteBuffer(Sampler[] samplers) {
			for (int i = 0; i < samplers.Length; i++) {
				var smpl = samplers[i];
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
					var oPan = (int)Math.Max(0, Math.Min(127, chOSC.Pan + mPan));
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
					sumL += osc.Value * PAN_L[oPan];
					sumR += osc.Value * PAN_R[oPan];
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
					var oPan = (int)Math.Max(0, Math.Min(127, chOSC.Pan + mPan));
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
					sumL += osc.Value * PAN_L[oPan];
					sumR += osc.Value * PAN_R[oPan];
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
