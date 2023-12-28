using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WINMM {
	public abstract class MidiLib : IDisposable {
		protected enum MIDIHDR_FLAG : uint {
			MHDR_NONE = 0,
			MHDR_DONE = 0x00000001,
			MHDR_PREPARED = 0x00000002,
			MHDR_INQUEUE = 0x00000004,
			MHDR_ISSTRM = 0x00000008
		}

		[StructLayout(LayoutKind.Sequential)]
		protected struct MIDIHDR {
			public IntPtr lpData;
			public uint dwBufferLength;
			public uint dwBytesRecorded;
			public uint dwUser;
			public MIDIHDR_FLAG dwFlags;
			public IntPtr lpNext;
			public uint reserved;
			public uint dwOffset;
			public uint dwReserved1;
			public uint dwReserved2;
			public uint dwReserved3;
			public uint dwReserved4;
		}

		protected const uint MIDI_MAPPER = unchecked((uint)-1);

		#region dynamic variable
		protected IntPtr mHandle;
		protected IntPtr[] mpMidiHeader;
		protected int mBufferCount;
		protected byte[] mBuffer;
		protected bool mDoStop = false;
		protected bool mStopped = true;
		#endregion

		#region property
		public bool Enabled { get; protected set; }
		public uint DeviceId { get; private set; } = MIDI_MAPPER;
		public int BufferSize { get; private set; }
		#endregion

		protected MidiLib(int bufferSize, int bufferCount) {
			BufferSize = bufferSize;
			mBufferCount = bufferCount;
			mBuffer = new byte[bufferSize];
		}

		protected void AllocHeader() {
			var defaultValue = new byte[BufferSize];
			mpMidiHeader = new IntPtr[mBufferCount];
			for (int i = 0; i < mBufferCount; ++i) {
				var hdr = new MIDIHDR() {
					dwFlags = MIDIHDR_FLAG.MHDR_NONE,
					dwBufferLength = (uint)BufferSize
				};
				hdr.lpData = Marshal.AllocHGlobal(BufferSize);
				Marshal.Copy(defaultValue, 0, hdr.lpData, BufferSize);
				mpMidiHeader[i] = Marshal.AllocHGlobal(Marshal.SizeOf<MIDIHDR>());
				Marshal.StructureToPtr(hdr, mpMidiHeader[i], true);
			}
		}

		protected void DisposeHeader() {
			for (int i = 0; i < mBufferCount; ++i) {
				if (mpMidiHeader[i] == IntPtr.Zero) {
					continue;
				}
				var hdr = Marshal.PtrToStructure<MIDIHDR>(mpMidiHeader[i]);
				if (hdr.lpData != IntPtr.Zero) {
					Marshal.FreeHGlobal(hdr.lpData);
				}
				Marshal.FreeHGlobal(mpMidiHeader[i]);
				mpMidiHeader[i] = IntPtr.Zero;
			}
		}

		public void Dispose() {
			Close();
		}

		public void SetDevice(uint deviceId) {
			var enable = Enabled;
			Close();
			DeviceId = deviceId;
			if (enable) {
				Open();
			}
		}

		public abstract void Open();

		public abstract void Close();
	}

	public abstract class MidiIn : MidiLib {
		enum MM_MIM {
			OPEN = 0x3C1,
			CLOSE = 0x3C2,
			DATA = 0x3C3,
			LONGDATA = 0x3C4
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct MIDIINCAPS {
			public ushort wMid;
			public ushort wPid;
			public uint vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szPname;
			public uint dwSupport;
		}

		delegate void DCallback(IntPtr hmi, MM_MIM uMsg, IntPtr dwInstance, uint dwParam1, uint dwParam2);
		DCallback mCallback;

		#region dll
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint midiInGetNumDevs();
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInGetDevCaps(uint uDeviceID, ref MIDIINCAPS pmic, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInOpen(ref IntPtr hmi, uint uDeviceID, DCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInClose(IntPtr hmi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInPrepareHeader(IntPtr hmi, IntPtr lpMidiHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMRESULT midiInUnprepareHeader(IntPtr hmi, IntPtr lpMidiHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInReset(IntPtr hmi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInAddBuffer(IntPtr hmi, IntPtr lpMidiHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInStart(IntPtr hmi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT midiInMessage(IntPtr hmi, uint uMsg, IntPtr dw1, IntPtr dw2);
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = midiInGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new MIDIINCAPS();
				var ret = midiInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMRESULT.MMSYSERR_NOERROR == ret) {
					list.Add(caps.szPname);
				} else {
					list.Add(ret.ToString());
				}
			}
			return list;
		}

		public MidiIn(int bufferSize = 1024, int bufferCount = 16) : base(bufferSize, bufferCount) {
			mCallback = Callback;
		}

		public override void Open() {
			Close();
			AllocHeader();
			var mr = midiInOpen(ref mHandle, DeviceId, mCallback, IntPtr.Zero);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				return;
			}
			for (int i = 0; i < mBufferCount; ++i) {
				midiInPrepareHeader(mHandle, mpMidiHeader[i], Marshal.SizeOf<MIDIHDR>());
				midiInAddBuffer(mHandle, mpMidiHeader[i], Marshal.SizeOf<MIDIHDR>());
			}
			midiInStart(mHandle);
		}

		public override void Close() {
			if (IntPtr.Zero == mHandle) {
				return;
			}
			mDoStop = true;
			for (int i = 0; i < 20 && !mStopped; i++) {
				Thread.Sleep(100);
			}
			for (int i = 0; i < mBufferCount; ++i) {
				midiInUnprepareHeader(mpMidiHeader[i], mHandle, Marshal.SizeOf<MIDIHDR>());
			}
			var mr = midiInReset(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				throw new Exception(mr.ToString());
			}
			mr = midiInClose(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				throw new Exception(mr.ToString());
			}
			mHandle = IntPtr.Zero;
			DisposeHeader();
		}

		void Callback(IntPtr hmi, MM_MIM uMsg, IntPtr dwInstance, uint dwParam1, uint dwParam2) {
			switch (uMsg) {
			case MM_MIM.OPEN:
				mStopped = false;
				Enabled = true;
				break;
			case MM_MIM.CLOSE:
				mDoStop = false;
				Enabled = false;
				break;
			case MM_MIM.DATA:
				if (mDoStop) {
					mStopped = true;
					break;
				}
				switch (dwParam1 & 0xF0) {
				case 0x80:
				case 0x90:
				case 0xB0:
				case 0xE0:
					Receive(new byte[] {
					(byte)(dwParam1 & 0xFF),
					(byte)((dwParam1 >> 8) & 0xFF),
					(byte)((dwParam1 >> 16) & 0xFF)
				});
					break;
				case 0xC0:
					Receive(new byte[] {
					(byte)(dwParam1 & 0xFF),
					(byte)((dwParam1 >> 8) & 0xFF)
				});
					break;
				}
				midiInAddBuffer(mHandle, dwInstance, Marshal.SizeOf<MIDIHDR>());
				break;
			case MM_MIM.LONGDATA:
				if (mDoStop) {
					mStopped = true;
					break;
				}
				var hdr = Marshal.PtrToStructure<MIDIHDR>(dwInstance);
				Marshal.Copy(hdr.lpData, mBuffer, 0, BufferSize);
				ReadBuffer();
				midiInAddBuffer(mHandle, dwInstance, Marshal.SizeOf<MIDIHDR>());
				break;
			default:
				break;
			}
		}

		protected abstract void Receive(byte[] message);

		protected virtual void ReadBuffer() { }
	}
}
