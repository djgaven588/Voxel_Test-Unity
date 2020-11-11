using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// The below code is heavily based off of
// https://gist.github.com/dogfuntom/cc881c8fc86ad43d55d8
// It has been modified in order to work how I want it to
// but it is still based off of the initial implementation.

public static class BlockRaycast
{
    public static (bool, Vector3Int, Vector3Int) Raycast(Vector3 origin, Vector3 direction, float radius, ChunkManager chunks)
    {
        // From "A Fast Voxel Traversal Algorithm for Ray Tracing"
        // by John Amanatides and Andrew Woo, 1987
        // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
        // <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
        // Extensions to the described algorithm:
        //   • Imposed a distance limit.
        //   • The face passed through to reach the current cube is provided to
        //     the callback.

        // The foundation of this algorithm is a parameterized representation of
        // the provided ray,
        //                    origin + t * direction,
        // except that t is not actually stored; rather, at any given point in the
        // traversal, we keep track of the *greater* t values which we would have
        // if we took a step sufficient to cross a cube boundary along that axis
        // (i.e. change the integer part of the coordinate) in the variables
        // tMaxX, tMaxY, and tMaxZ.

        // Handle errors regarding exactly being an integer.
        // Weird, yes I know, don't touch it.
        origin.x += origin.x % 1 < 0.001f ? 0.001f : 0f;
        origin.y += origin.y % 1 < 0.001f ? 0.001f : 0f;
        origin.z += origin.z % 1 < 0.001f ? 0.001f : 0f;

        Vector3Int currentCube = new Vector3Int((int)Math.Floor(origin.x), (int)Math.Floor(origin.y), (int)Math.Floor(origin.z));

        // Break out direction vector.
        var dx = direction.x;
        var dy = direction.y;
        var dz = direction.z;

        // Direction to increment x,y,z when stepping.
        var stepX = DirectionToIncrement(dx);
        var stepY = DirectionToIncrement(dy);
        var stepZ = DirectionToIncrement(dz);

        // See description above. The initial values depend on the fractional
        // part of the origin.
        var tMaxX = DistanceToGridIntersect(origin.x, dx);
        var tMaxY = DistanceToGridIntersect(origin.y, dy);
        var tMaxZ = DistanceToGridIntersect(origin.z, dz);

        // The change in t when taking a step (always positive).
        var tDeltaX = stepX / dx;
        var tDeltaY = stepY / dy;
        var tDeltaZ = stepZ / dz;

        // Buffer for reporting faces to the callback.
        var face = Vector3Int.zero;

        // Avoids an infinite loop.
        if (dx == 0 && dy == 0 && dz == 0)
            throw new Exception("Ray-cast in zero direction!");

        // Rescale from units of 1 cube-edge to units of 'direction' so we can
        // compare with 't'.
        radius /= (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

        while (true)
        {
            // Check if current position is a hit block
            if (IsBlockHit(currentCube, chunks, origin, direction, out Vector3Int boundsFace))
            {
                return (true, currentCube, boundsFace);
            }

            // tMaxX stores the t-value at which we cross a cube boundary along the
            // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
            // chooses the closest cube boundary. Only the first case of the four
            // has been commented in detail.
            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    if (tMaxX > radius)
                        return (false, Vector3Int.zero, Vector3Int.zero);
                    // Update which cube we are now in.
                    currentCube.x += stepX;
                    // Adjust tMaxX to the next X-oriented boundary crossing.
                    tMaxX += tDeltaX;
                    // Record the normal vector of the cube face we entered.
                    face.x = -stepX;
                    face.y = 0;
                    face.z = 0;
                }
                else
                {
                    if (tMaxZ > radius)
                        return (false, Vector3Int.zero, Vector3Int.zero);
                    currentCube.z += stepZ;
                    tMaxZ += tDeltaZ;
                    face.x = 0;
                    face.y = 0;
                    face.z = -stepZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    if (tMaxY > radius)
                        return (false, Vector3Int.zero, Vector3Int.zero);
                    currentCube.y += stepY;
                    tMaxY += tDeltaY;
                    face.x = 0;
                    face.y = -stepY;
                    face.z = 0;
                }
                else
                {
                    // Identical to the second case, repeated for simplicity in
                    // the conditionals.
                    if (tMaxZ > radius)
                        return (false, Vector3Int.zero, Vector3Int.zero);
                    currentCube.z += stepZ;
                    tMaxZ += tDeltaZ;
                    face.x = 0;
                    face.y = 0;
                    face.z = -stepZ;
                }
            }
        }
    }

    private static bool IsBlockHit(Vector3Int pos, ChunkManager chunks, Vector3 origin, Vector3 direction, out Vector3Int boundsFace)
    {
        if (chunks.Chunks.TryGetValue(WorldToChunk(pos), out Chunk chunk))
        {
            Block block = chunk.GetBlock(Chunk.BlockToIndex(WorldToBlock(pos)));

            if (block.CanTouch && RayCanHitBlock(block, pos, origin, direction, out float distance))
            {
                // Find the face we hit.
                Bounds bounds = block.GetBounds(pos);
                Vector3 hitPoint = origin + direction * distance;

                float leftBounds = bounds.center.x - bounds.extents.x;
                float rightBounds = bounds.center.x + bounds.extents.x;

                float forwardBounds = bounds.center.z + bounds.extents.z;
                float backBounds = bounds.center.z - bounds.extents.z;

                float topBounds = bounds.center.y + bounds.extents.y;
                float bottomBounds = bounds.center.y - bounds.extents.y;

                if (Mathf.Approximately(rightBounds, hitPoint.x))
                {
                    boundsFace = Vector3Int.right;
                }
                else if (Mathf.Approximately(leftBounds, hitPoint.x))
                {
                    boundsFace = Vector3Int.left;
                }
                else if (Mathf.Approximately(forwardBounds, hitPoint.z))
                {
                    boundsFace = new Vector3Int(0, 0, 1);
                }
                else if (Mathf.Approximately(backBounds, hitPoint.z))
                {
                    boundsFace = new Vector3Int(0, 0, -1);
                }
                else if (Mathf.Approximately(topBounds, hitPoint.y))
                {
                    boundsFace = new Vector3Int(0, 1, 0);
                }
                else if (Mathf.Approximately(bottomBounds, hitPoint.y))
                {
                    boundsFace = new Vector3Int(0, -1, 0);
                }
                else
                {
                    boundsFace = Vector3Int.zero;
                }

                return true;
            }
            else
            {
                boundsFace = Vector3Int.zero;
                return false;
            }
        }

        boundsFace = Vector3Int.zero;

        return true;
    }

    private static bool RayCanHitBlock(Block block, Vector3Int pos, Vector3 rayOrigin, Vector3 rayDirection, out float distance)
    {
        return block.GetBounds(pos).IntersectRay(new Ray(rayOrigin, rayDirection), out distance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3Int WorldToChunk(Vector3Int pos)
    {
        Vector3Int chunkPos = new Vector3Int(pos.x, pos.y, pos.z);
        chunkPos.x >>= Chunk.CHUNK_LOG_SIZE;
        chunkPos.y >>= Chunk.CHUNK_LOG_SIZE;
        chunkPos.z >>= Chunk.CHUNK_LOG_SIZE;

        return chunkPos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToBlock(Vector3Int pos)
    {
        Vector3Int block = new Vector3Int(pos.x, pos.y, pos.z);
        block.x &= Chunk.CHUNK_SIZE_MINUS_ONE;
        block.y &= Chunk.CHUNK_SIZE_MINUS_ONE;
        block.z &= Chunk.CHUNK_SIZE_MINUS_ONE;
        return block;
    }

    private static int DirectionToIncrement(float x)
    {
        return x >= 0 ? 1 : x < 0 ? -1 : 0;
    }

    private static float DistanceToGridIntersect(float s, float ds)
    {
        // Some kind of edge case, see:
        // http://gamedev.stackexchange.com/questions/47362/cast-ray-to-select-block-in-voxel-game#comment160436_49423
        var sIsInteger = Math.Round(s) == s;
        if (ds < 0 && sIsInteger)
            return 0;

        return (float)(ds > 0 ? CustomCeil(s) - s : s - Math.Floor(s)) / Math.Abs(ds);
    }

    private static float CustomCeil(float s)
    {
        if (s == 0f)
            return 1f;
        else
            return (float)Math.Ceiling(s);
    }
}
