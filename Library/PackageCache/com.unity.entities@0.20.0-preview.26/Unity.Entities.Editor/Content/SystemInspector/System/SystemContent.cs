using System;

namespace Unity.Entities.Editor
{
    class SystemContent
    {
        public SystemContent(World world, SystemProxy system)
        {
            World = world;
            System = system;
        }

        public World World { get; }
        public SystemProxy System { get; }
    }
}
