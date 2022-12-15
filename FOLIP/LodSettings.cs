using Mutagen.Bethesda.Synthesis.Settings;

namespace FOLIP
{
	internal class LodSettings
	{
		[SynthesisOrder]
		[SynthesisSettingName("Verbose")]
		[SynthesisTooltip("Enable verbose messages (typically only for mod author knowledge).")]
		public bool verboseConsoleLog = false;
	}
}
