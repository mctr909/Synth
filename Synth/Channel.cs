using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synth {
	internal class Channel {
		public int Num { get; private set; }

		public Instruments.EG EG;
		public Instruments.LFO LFO1;
		public Instruments.LFO LFO2;
		public Instruments.OSC[] OSC;

		public double Gain;
		public double Pitch;
		public double Pan;
		public double Resonance;
		public bool Hold;

		public double[] InputL = null;
		public double[] InputR = null;
	}
}
