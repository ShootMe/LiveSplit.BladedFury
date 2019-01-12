using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
namespace LiveSplit.BladedFury {
	public partial class SplitterMemory {
		private static ProgramPointer Main = new ProgramPointer(AutoDeref.Single, DerefType.Int64, new ProgramSignature(PointerVersion.Steam, "55488BEC564883EC08488BF148B8????????????????488B0833D24883EC2049BB????????????????41FFD34883C42085C00f84????????4883EC20", 14));
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		public DateTime LastHooked;

		public SplitterMemory() {
			LastHooked = DateTime.MinValue;
		}

		public bool Loading() {
			//Main.Instance.SceneMgr.bfadeInAnim/bfadeOutAnim
			return !IsHooked || Main.Read<int>(Program, 0x0, 0x28, 0x68) != 0;
		}
		public string SceneName() {
			//Main.Instance.SceneMgr.activeScene.sceneName
			return Main.Read(Program, 0x0, 0x28, 0x50, 0x10, 0x0);
		}
		public string SaveName() {
			//Main.Instance.mPlayerDataManager.saveName
			return Main.Read(Program, 0x0, 0x80, 0x18, 0x0);
		}
		public int BossInfo() {
			//Main.Instance.monsterList
			IntPtr monsters = (IntPtr)Main.Read<ulong>(Program, 0x0, 0xb0);
			int count = Program.Read<int>(monsters, 0x18);
			if (count > 0) {
				monsters = (IntPtr)Program.Read<ulong>(monsters, 0x10);
				for (int i = 0; i < count; i++) {
					IntPtr monster = (IntPtr)Program.Read<ulong>(monsters, 0x20 + (i * 8));

					bool isBoss = Program.Read<bool>(monster, 0x238, 0x88);
					if (isBoss) {
						float curHp = Program.Read<float>(monster, 0x238, 0x4c);
						return curHp > 0 ? Program.Read<int>(monster, 0x238, 0x38) : -1;
					}
				}
			}
			return -1;
		}
		public string MonsterName(int id) {
			if (id < 0) { return string.Empty; }

			//Main.Instance.m_DBMgr.monsterStaticData
			IntPtr monsterData = (IntPtr)Main.Read<ulong>(Program, 0x0, 0x58, 0x18);
			int count = Program.Read<int>(monsterData, 0x38);
			if (count > 0) {
				monsterData = (IntPtr)Program.Read<ulong>(monsterData, 0x28);
				int tableCount = Program.Read<int>(monsterData, 0x18);
				for (int i = 0; i < tableCount && count > 0; i++) {
					IntPtr monster = (IntPtr)Program.Read<ulong>(monsterData, 0x20 + (i * 8));
					if (monster == IntPtr.Zero) { continue; }

					count--;
					int mid = Program.Read<int>(monster, 0x48);
					if (mid == id) {
						return Path.GetFileNameWithoutExtension(Program.ReadString(monster, 0x10, 0x0));
					}
				}
			}
			return string.Empty;
		}
		public bool HookProcess() {
			IsHooked = Program != null && !Program.HasExited;
			if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
				LastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("chopghost");
				Program = processes != null && processes.Length > 0 ? processes[0] : null;

				if (Program != null && !Program.HasExited) {
					MemoryReader.Update64Bit(Program);
					IsHooked = true;
				}
			}

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
}