using UnityEditor;

namespace Unity.Entities.Editor
{
    static class EntitiesJournalingMenuItem
    {
        const string k_Name = "DOTS/Entities Journaling/Enable Entities Journaling";

        [MenuItem(k_Name)]
        static void ToggleEntitiesJournaling()
        {
            EntitiesJournaling.Enabled = !EntitiesJournaling.Enabled;
        }

        [MenuItem(k_Name, true)]
        static bool ValidateToggleEntitiesJournaling()
        {
            Menu.SetChecked(k_Name, EntitiesJournaling.Enabled);
            return true;
        }
    }
}
