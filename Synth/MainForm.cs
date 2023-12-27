using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Synth {
	public partial class MainForm : Form {
		Playback mPlayback;

		public MainForm() {
			InitializeComponent();
			mPlayback = new Playback(44100, 256);
			mPlayback.Open();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			mPlayback.Dispose();
		}

		private void button1_MouseDown(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x90, 0x20, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x27, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x30, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x2C + 12, 0x1F });
		}

		private void button1_MouseUp(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x80, 0x20 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x27 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x30 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x2C + 12 });
		}

		private void button2_MouseDown(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x90, 0x20 + 5, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x27 + 5, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x30 + 5, 0x1F });
		}

		private void button2_MouseUp(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x80, 0x20 + 5 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x27 + 5 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x30 + 5 });
		}

		private void button3_MouseDown(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x90, 0x20 + 7, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x27 + 7, 0x1F });
			Channel.SendMessage(0, new byte[] { 0x90, 0x30 + 7, 0x1F });
		}

		private void button3_MouseUp(object sender, MouseEventArgs e) {
			Channel.SendMessage(0, new byte[] { 0x80, 0x20 + 7 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x27 + 7 });
			Channel.SendMessage(0, new byte[] { 0x80, 0x30 + 7 });
		}
	}
}
