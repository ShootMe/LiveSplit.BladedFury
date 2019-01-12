using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
namespace LiveSplit.BladedFury {
	public class SplitterComponent : IComponent {
		public TimerModel Model { get; set; }
		public string ComponentName { get { return "Bladed Fury Autosplitter " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3); } }
		public IDictionary<string, Action> ContextMenuControls { get { return null; } }
		private static string LOGFILE = "_BladedFury.txt";
		private Dictionary<LogObject, string> currentValues = new Dictionary<LogObject, string>();
		private SplitterMemory mem;
		private int currentSplit = 0, lastLogCheck = 0;
		private bool hasLog = false;
		private SplitterSettings settings;
		private string lastSceneName, lastSaveName, lastBossName;
		private Thread updateLoop;
		public SplitterComponent(LiveSplitState state) {
			mem = new SplitterMemory();
			settings = new SplitterSettings();
			foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
				currentValues[key] = "";
			}

			if (state != null) {
				Model = new TimerModel() { CurrentState = state };
				Model.InitializeGameTime();
				Model.CurrentState.IsGameTimePaused = true;
				state.OnReset += OnReset;
				state.OnPause += OnPause;
				state.OnResume += OnResume;
				state.OnStart += OnStart;
				state.OnSplit += OnSplit;
				state.OnUndoSplit += OnUndoSplit;
				state.OnSkipSplit += OnSkipSplit;

				updateLoop = new Thread(UpdateLoop);
				updateLoop.IsBackground = true;
				updateLoop.Start();
			}
		}
		private void UpdateLoop() {
			while (updateLoop != null) {
				try {
					GetValues();
				} catch (Exception ex) {
					WriteLog(ex.ToString());
				}
				Thread.Sleep(8);
			}
		}
		public void GetValues() {
			if (!mem.HookProcess()) { return; }

			string scene = mem.SceneName();

			if (Model != null) {
				HandleSplits(scene);
			}

			lastSceneName = string.IsNullOrEmpty(scene) ? lastSceneName : scene;

			LogValues();
		}
		private void HandleSplits(string scene) {
			bool shouldSplit = false;
			bool loading = mem.Loading();

			if (currentSplit < Model.CurrentState.Run.Count && currentSplit < settings.Splits.Count) {
				SplitName split = settings.Splits[currentSplit];

				switch (split) {
					case SplitName.StartGame:
						string saveName = mem.SaveName();
						if (!string.IsNullOrEmpty(saveName)) {
							DateTime saveDate = DateTime.MinValue;
							try {
								saveDate = DateTime.FromBinary(Convert.ToInt64(saveName.Substring(4), 16));
							} catch { }
							shouldSplit = mem.LastHooked.AddSeconds(5) < DateTime.Now && scene == "_MainMenu" && saveName != lastSaveName && saveDate >= DateTime.Now.AddSeconds(-5);
						}
						lastSaveName = saveName;
						break;
					case SplitName.DukeKang: shouldSplit = !loading && scene == "0104" && BossSplit("Bs004"); break;
					case SplitName.TianRangju: shouldSplit = !loading && scene == "0707" && BossSplit("Bs009"); break;
				}
			}

			Model.CurrentState.IsGameTimePaused = Model.CurrentState.CurrentPhase != TimerPhase.Running || loading || scene == "TransitionScene";

			HandleSplit(shouldSplit, false);
		}
		private bool BossSplit(string boss) {
			string bossName = mem.MonsterName(mem.BossInfo());
			bool shouldSplit = lastBossName == boss && string.IsNullOrEmpty(bossName);
			lastBossName = bossName;
			return shouldSplit;
		}
		private void HandleSplit(bool shouldSplit, bool shouldReset = false) {
			if (shouldReset) {
				if (currentSplit > 0) {
					Model.Reset();
				}
			} else if (shouldSplit) {
				if (currentSplit <= 0) {
					Model.Start();
				} else {
					Model.Split();
				}
			}
		}
		private void LogValues() {
			if (lastLogCheck == 0) {
				hasLog = File.Exists(LOGFILE);
				lastLogCheck = 300;
			}
			lastLogCheck--;

			if (hasLog || !Console.IsOutputRedirected) {
				string prev = string.Empty, curr = string.Empty;
				foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
					prev = currentValues[key];

					switch (key) {
						case LogObject.CurrentSplit: curr = currentSplit.ToString(); break;
						case LogObject.SceneName: curr = lastSceneName; break;
						case LogObject.Loading: curr = mem.Loading().ToString(); break;
						case LogObject.SaveName: curr = mem.SaveName(); break;
						case LogObject.BossInfo: curr = mem.MonsterName(mem.BossInfo()); break;
						default: curr = string.Empty; break;
					}

					if (prev == null) { prev = string.Empty; }
					if (curr == null) { curr = string.Empty; }
					if (!prev.Equals(curr)) {
						WriteLogWithTime(key.ToString() + ": ".PadRight(16 - key.ToString().Length, ' ') + prev.PadLeft(25, ' ') + " -> " + curr);

						currentValues[key] = curr;
					}
				}
			}
		}
		private void WriteLog(string data) {
			lock (LOGFILE) {
				if (hasLog || !Console.IsOutputRedirected) {
					if (!Console.IsOutputRedirected) {
						Console.WriteLine(data);
					}
					if (hasLog) {
						using (StreamWriter wr = new StreamWriter(LOGFILE, true)) {
							wr.WriteLine(data);
						}
					}
				}
			}
		}
		private void WriteLogWithTime(string data) {
			WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + (Model != null && Model.CurrentState.CurrentTime.RealTime.HasValue ? " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) : "") + ": " + data);
		}
		public void Update(IInvalidator invalidator, LiveSplitState lvstate, float width, float height, LayoutMode mode) {
		}
		public void OnReset(object sender, TimerPhase e) {
			currentSplit = 0;
			WriteLog("---------Reset----------------------------------");
		}
		public void OnResume(object sender, EventArgs e) {
			WriteLog("---------Resumed--------------------------------");
		}
		public void OnPause(object sender, EventArgs e) {
			WriteLog("---------Paused---------------------------------");
		}
		public void OnStart(object sender, EventArgs e) {
			currentSplit = 1;
			Model.CurrentState.SetGameTime(TimeSpan.Zero);
			Model.CurrentState.IsGameTimePaused = true;
			WriteLog("---------New Game " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3) + "-------------------------");
		}
		public void OnUndoSplit(object sender, EventArgs e) {
			currentSplit--;
			WriteLog("---------Undo-----------------------------------");
		}
		public void OnSkipSplit(object sender, EventArgs e) {
			currentSplit++;
			WriteLog("---------Skip-----------------------------------");
		}
		public void OnSplit(object sender, EventArgs e) {
			currentSplit++;
			WriteLog("---------Split----------------------------------");
		}
		public Control GetSettingsControl(LayoutMode mode) { return settings; }
		public void SetSettings(XmlNode document) { settings.SetSettings(document); }
		public XmlNode GetSettings(XmlDocument document) { return settings.UpdateSettings(document); }
		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }
		public float HorizontalWidth { get { return 0; } }
		public float MinimumHeight { get { return 0; } }
		public float MinimumWidth { get { return 0; } }
		public float PaddingBottom { get { return 0; } }
		public float PaddingLeft { get { return 0; } }
		public float PaddingRight { get { return 0; } }
		public float PaddingTop { get { return 0; } }
		public float VerticalHeight { get { return 0; } }
		public void Dispose() {
			if (updateLoop != null) {
				updateLoop = null;
			}
			if (Model != null) {
				Model.CurrentState.OnReset -= OnReset;
				Model.CurrentState.OnPause -= OnPause;
				Model.CurrentState.OnResume -= OnResume;
				Model.CurrentState.OnStart -= OnStart;
				Model.CurrentState.OnSplit -= OnSplit;
				Model.CurrentState.OnUndoSplit -= OnUndoSplit;
				Model.CurrentState.OnSkipSplit -= OnSkipSplit;
				Model = null;
			}
		}
	}
}