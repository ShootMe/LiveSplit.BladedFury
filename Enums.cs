using System.ComponentModel;
namespace LiveSplit.BladedFury {
	public enum LogObject {
		CurrentSplit,
		SceneName,
		SaveName,
		Loading,
		BossInfo,
	}
	public enum SplitName {
		[Description("Manual Split"), ToolTip("Does not split automatically. Use this for custom splits not yet defined.")]
		ManualSplit,
		[Description("Start Game"), ToolTip("Starts the game timer")]
		StartGame,
		[Description("Duke Kang (Boss)"), ToolTip("Splits when the boss is killed")]
		DukeKang,
		[Description("General Wu 1 (Boss)"), ToolTip("Splits when the boss is killed")]
		GeneralWu1,
		[Description("General Wu 2 (Boss)"), ToolTip("Splits when the boss is killed")]
		GeneralWu2,
		[Description("Xia Zhengshu 1 (Boss)"), ToolTip("Splits when the boss is killed")]
		XiaZhengshu1,
		[Description("Wen (Boss)"), ToolTip("Splits when the boss is killed")]
		Wen,
		[Description("Bogu 1 (Boss)"), ToolTip("Splits when the boss is killed")]
		Bogu1,
		[Description("Emperor Of Zhou (Boss)"), ToolTip("Splits when the boss is killed")]
		EmperorOfZhou,
		[Description("The Serpent (Boss)"), ToolTip("Splits when the boss is killed")]
		TheSerpent,
		[Description("General Wu 3 (Boss)"), ToolTip("Splits when the boss is killed")]
		GeneralWu3,
		[Description("Xia Zhengshu 2 (Boss)"), ToolTip("Splits when the boss is killed")]
		XiaZhengshu2,
		[Description("Bogu 2 (Boss)"), ToolTip("Splits when the boss is killed")]
		Bogu2,
		[Description("Tian Rangju (Boss)"), ToolTip("Splits when the boss is killed")]
		TianRangju
	}
}