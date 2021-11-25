#if !UNITY_DOTSRUNTIME
using Unity.Entities;
using UnityEngine;

namespace Unity.Scenes
{
#if UNITY_EDITOR || PLATFORM_SWITCH
    [DisableAutoCreation]
#endif
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SceneSystemGroup))]
    class LiveLinkRuntimeSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            LiveLinkUtility.LiveLinkBoot();
            Enabled = LiveLinkUtility.LiveLinkEnabled;
            if (Enabled)
            {
                World.GetOrCreateSystem<SceneSystem>().BuildConfigurationGUID = LiveLinkUtility.BuildConfigurationGUID;
            }
        }
    }
}
#endif
