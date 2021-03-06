using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Codec = Unity.Core.Compression.Codec;

namespace Unity.Entities
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct RuntimeBlobHeaderRef
    {
        [FieldOffset(0)]
        internal long m_BlobAssetRefStorage;
        public ref DotsSerialization.BlobHeader Value => ref UnsafeUtility.As<long, BlobAssetReference<DotsSerialization.BlobHeader>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator RuntimeBlobHeaderRef(BlobAssetReference<DotsSerialization.BlobHeader> assetRef)
        {
            RuntimeBlobHeaderRef ret = default;
            UnsafeUtility.As<long, BlobAssetReference<DotsSerialization.BlobHeader>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }
        public static implicit operator BlobAssetReference<DotsSerialization.BlobHeader>(RuntimeBlobHeaderRef clip)
        {
            return UnsafeUtility.As<long, BlobAssetReference<DotsSerialization.BlobHeader>>(ref clip.m_BlobAssetRefStorage);
        }

        public unsafe RuntimeBlobHeaderRef Resolve(BlobAssetOwner blobAssetOwner)
        {
            var blobAssetRef = new BlobAssetReference<DotsSerialization.BlobHeader>();
            blobAssetRef.m_data.m_Ptr = (byte*) blobAssetOwner.BlobAssetBatchPtr + m_BlobAssetRefStorage;
            return blobAssetRef;
        }
    }

    [Serializable]
    public struct SceneSectionData : IComponentData
    {
        public Hash128          SceneGUID;
        public int              SubSectionIndex;
        public int              FileSize;
        public int              ObjectReferenceCount;
        public MinMaxAABB       BoundingVolume;
        internal Codec          Codec;
        internal int            DecompressedFileSize;
        internal RuntimeBlobHeaderRef BlobHeader;
    }

    // This component identifies the entity which holds the metadata components belonging to the section with the specified SceneSectionIndex
    // These metadata components will be serialized into the entity scene header and be added to the section entities after the scene is resolved at runtime
    public struct SectionMetadataSetup : ISharedComponentData
    {
        public int SceneSectionIndex;
    }

    public struct SceneReference : IComponentData, IEquatable<SceneReference>
    {
        public Hash128 SceneGUID;

        public SceneReference(EntitySceneReference sceneReference)
        {
            SceneGUID = sceneReference.SceneId.AssetId;
        }

        public bool Equals(SceneReference other)
        {
            return SceneGUID.Equals(other.SceneGUID);
        }

        public override int GetHashCode()
        {
            return SceneGUID.GetHashCode();
        }
    }

    public struct PrefabRoot : IComponentData
    {
        public Entity Root;
    }

    [System.Serializable]
    public struct SceneSection : ISharedComponentData, IEquatable<SceneSection>
    {
        public Hash128        SceneGUID;
        public int            Section;

        public bool Equals(SceneSection other)
        {
            return SceneGUID.Equals(other.SceneGUID) && Section == other.Section;
        }

        public override int GetHashCode()
        {
            return (SceneGUID.GetHashCode() * 397) ^ Section;
        }
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class PostLoadCommandBuffer : IComponentData, IDisposable, ICloneable
    {
        public EntityCommandBuffer CommandBuffer;
        private int RefCount;
        public PostLoadCommandBuffer()
        {
            RefCount = 1;
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref RefCount) == 0)
                CommandBuffer.Dispose();
        }

        public object Clone()
        {
            Interlocked.Increment(ref RefCount);
            return this;
        }
    }
#endif

    [Flags]
    public enum SceneLoadFlags
    {
        /// <summary>
        /// Prevents adding a RequestSceneLoaded to the SubScene section entities when it gets created. If loading a GameObject scene, setting this flag is equivalent to setting activateOnlLoad to false.
        /// </summary>
        DisableAutoLoad = 1,
        /// <summary>
        /// Wait for the SubScene to be fully converted (only relevant for Editor and LiveLink) and its header loaded
        /// </summary>
        BlockOnImport = 2,
        /// <summary>
        /// Disable asynchronous streaming, SubScene section will be fully loaded during the next update of the streaming system
        /// </summary>
        BlockOnStreamIn = 4,
        // TODO: Remove this RemovedAfter 2021-02-05 (https://unity3d.atlassian.net/browse/DOTS-3380)
        // SceneLoadFlags.LoadAdditive is deprecated. Scenes loaded through the SceneSystem are always loaded Additively. This previously was only used when using LiveLink with GameObjects.
        /// <summary>
        /// [DEPRECATED] Set whether to load additive or not. This only applies to GameObject based scenes, not subscenes.
        /// </summary>
        LoadAdditive = 8,
        /// <summary>
        /// Loads a new instance of the subscene
        /// </summary>
        NewInstance = 16,
        /// <summary>
        /// Temporary flag to indicate that the scene is a GameObject based scene.  Once addressables are in place, this information will be stored there.
        /// </summary>
        LoadAsGOScene = 512,
    }

    public struct RequestSceneLoaded : IComponentData
    {
        public SceneLoadFlags LoadFlags;
    }
}
