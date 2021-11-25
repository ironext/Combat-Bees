using Unity.Properties;
using Unity.Serialization;

namespace Unity.Entities.Editor
{
    [DOTSEditorPreferencesSetting(k_SectionName)]
    class EntitiesJournalingSettings : ISetting
    {
        const string k_SectionName = "Entities Journaling";

        [CreateProperty, DontSerialize]
        public bool Enabled
        {
            get => EntitiesJournaling.Enabled;
            set => EntitiesJournaling.Enabled = value;
        }

        [CreateProperty, DontSerialize]
        public int TotalMemoryMB
        {
            get => EntitiesJournaling.TotalMemoryMB;
            set => EntitiesJournaling.TotalMemoryMB = value;
        }

        public void OnSettingChanged(PropertyPath path)
        {
        }
    }
}
