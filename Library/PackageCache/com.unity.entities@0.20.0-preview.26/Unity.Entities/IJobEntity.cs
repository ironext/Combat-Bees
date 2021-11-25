// Please refer to the README.md document in the IJobEntitiesForEach example in the Samples project for more information.

using System;
using Unity.Jobs;

#if DOTS_EXPERIMENTAL
namespace Unity.Entities
{
    /// <summary>
    /// Any type which implements this interface and also contains an `Execute()` method (with any number of parameters)
    /// will trigger source generation of a corresponding IJobEntityBatch type. The generated IJobEntityBatch type in turn
    /// invokes the Execute() method on the IJobEntity type with the appropriate arguments.
    /// </summary>
    public interface IJobEntity
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EntityInQueryIndex : Attribute
    {
    }

    public static class IJobEntityExtensions
    {
        // Mirrors all of the schedule methods for IJobEntityBatch, except we must also have a version that takes no query as IJobEntity can generate the query for you
        // IJobEntityBatch method is first, follow by its No Query version
        // Currently missing the limitToEntityArray versions
        // These methods must all be replicated in the generated job struct to prevent compiler ambiguity

        // These methods keep the full type names so that it can be easily copy pasted into JobEntityDescriptionSourceFactor.cs when updated

        public static Unity.Jobs.JobHandle Schedule<T>(this T jobData, Unity.Entities.EntityQuery query, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static Unity.Jobs.JobHandle Schedule<T>(this T jobData, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();

        public static Unity.Jobs.JobHandle ScheduleByRef<T>(this ref T jobData, Unity.Entities.EntityQuery query, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static Unity.Jobs.JobHandle ScheduleByRef<T>(this ref T jobData, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();

        public static Unity.Jobs.JobHandle ScheduleParallel<T>(this T jobData, Unity.Entities.EntityQuery query, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static Unity.Jobs.JobHandle ScheduleParallel<T>(this T jobData, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();

        public static Unity.Jobs.JobHandle ScheduleParallelByRef<T>(this ref T jobData, Unity.Entities.EntityQuery query, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static Unity.Jobs.JobHandle ScheduleParallelByRef<T>(this ref T jobData, Unity.Jobs.JobHandle dependsOn = default(JobHandle)) where T : struct, IJobEntity => __ThrowCodeGenException();

        public static void Run<T>(this T jobData, Unity.Entities.EntityQuery query) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static void Run<T>(this T jobData) where T : struct, IJobEntity => __ThrowCodeGenException();

        public static void RunByRef<T>(this ref T jobData, Unity.Entities.EntityQuery query) where T : struct, IJobEntity => __ThrowCodeGenException();
        public static void RunByRef<T>(this ref T jobData) where T : struct, IJobEntity => __ThrowCodeGenException();

        static Unity.Jobs.JobHandle __ThrowCodeGenException() => throw new Exception("This method should have been replaced by source gen.");

    }
}
#endif
