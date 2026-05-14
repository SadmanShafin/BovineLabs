using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    [BurstCompile]
    public static class PrimitiveSetFactory
    {
        public static bool TryCreateCardinal8(Allocator allocator, out PrimitiveSet result)
        {
            result = new PrimitiveSet(64, allocator);
            int2[] offsets =
            {
                new(0, -1),
                new(1, -1),
                new(1, 0),
                new(1, 1),
                new(0, 1),
                new(-1, 1),
                new(-1, 0),
                new(-1, -1)
            };

            var primId = 0;
            for (var theta = 0; theta < 8; theta++)
            for (var dir = 0; dir < 8; dir++)
            {
                var offset = offsets[dir];
                var length = math.length(new float2(offset.x, offset.y));

                var sweptI = new NativeArray<int>(1, allocator);
                var sweptJ = new NativeArray<int>(1, allocator);
                sweptI[0] = offset.x;
                sweptJ[0] = offset.y;

                var prim = new MotionPrimitive(
                    primId++,
                    theta,
                    offset,
                    dir,
                    length,
                    sweptI,
                    sweptJ
                );
                result.Add(prim);
            }

            return true;
        }

        public static bool TryCreateExtended8(Allocator allocator, out PrimitiveSet result)
        {
            result = new PrimitiveSet(24, allocator);
            int2[] dirs =
            {
                new(0, -1), new(1, -1), new(1, 0), new(1, 1),
                new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1)
            };

            var primId = 0;
            for (var theta = 0; theta < 8; theta++)
            {
                var dir = dirs[theta];
                var baseLen = math.length(new float2(dir.x, dir.y));


                var si1 = new NativeArray<int>(1, allocator);
                var sj1 = new NativeArray<int>(1, allocator);
                si1[0] = dir.x;
                sj1[0] = dir.y;
                result.Add(new MotionPrimitive(primId++, theta, dir, theta, baseLen, si1, sj1));


                var si2 = new NativeArray<int>(2, allocator);
                var sj2 = new NativeArray<int>(2, allocator);
                si2[0] = dir.x;
                sj2[0] = dir.y;
                si2[1] = dir.x * 2;
                sj2[1] = dir.y * 2;
                result.Add(new MotionPrimitive(primId++, theta, dir * 2, theta, baseLen * 2, si2, sj2));


                var nextTheta = (theta + 1) % 8;
                var nextDir = dirs[nextTheta];
                var endOff = dir + nextDir;
                var si3 = new NativeArray<int>(3, allocator);
                var sj3 = new NativeArray<int>(3, allocator);
                si3[0] = dir.x;
                sj3[0] = dir.y;
                si3[1] = dir.x + nextDir.x;
                sj3[1] = dir.y + nextDir.y;
                si3[2] = endOff.x;
                sj3[2] = endOff.y;
                var arcLen = math.length(new float2(endOff.x, endOff.y));
                result.Add(new MotionPrimitive(primId++, theta, endOff, nextTheta, arcLen, si3, sj3));
            }

            return true;
        }
    }
}