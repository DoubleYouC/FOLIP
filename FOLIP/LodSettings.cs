using Mutagen.Bethesda.Synthesis.Settings;

namespace FOLIP
{
	internal class LodSettings
	{
		[SynthesisOrder]
		[SynthesisSettingName("Moveable Statics")]
		[SynthesisTooltip("Enable LOD for moveable static references.")]
		public bool moveableStatics = false;

        [SynthesisOrder]
        [SynthesisSettingName("Force LOD8")]
        [SynthesisTooltip("Forces LOD4 to LOD8 where missing.")]
        public bool forceLOD8 = false;


        [SynthesisOrder]
		[SynthesisSettingName("Dev Settings")]
		[SynthesisTooltip("Convenience settings for development purposes.")]
		public DevSettings devSettings = new();
	}

	internal class DevSettings
    {
		[SynthesisOrder]
		[SynthesisSettingName("Verbose")]
		[SynthesisTooltip("Enable verbose messages (typically only for mod author knowledge).")]
		public bool verboseConsoleLog = false;

		[SynthesisOrder]
		[SynthesisSettingName("Enable Dev Code")]
		[SynthesisTooltip("Enables dev code (keep this disabled).")]
		public bool devCode = false;
	}
}
