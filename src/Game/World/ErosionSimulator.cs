using System;

namespace CorditeWars.Game.World;

/// <summary>
/// Simulates hydraulic and thermal erosion on an elevation map.
/// Produces natural-looking terrain with gullies, sediment deposits, and weathered ridgelines.
/// All operations are deterministic and produce bit-identical results for the same inputs.
/// </summary>
public sealed class ErosionSimulator
{
    // Hydraulic erosion parameters
    private const float Inertia = 0.05f;
    private const float SedimentCapacityFactor = 4.0f;
    private const float MinSedimentCapacity = 0.01f;
    private const float ErodeSpeed = 0.3f;
    private const float DepositSpeed = 0.3f;
    private const float EvaporateSpeed = 0.01f;
    private const float Gravity = 4.0f;
    private const int MaxDropletLifetime = 30;

    /// <summary>
    /// Applies iterative hydraulic erosion to the elevation map.
    /// Each droplet traces a path downhill, eroding material and depositing
    /// sediment based on carrying capacity and velocity.
    /// </summary>
    /// <param name="elevation">Mutable elevation array (width × height).</param>
    /// <param name="width">Grid width.</param>
    /// <param name="height">Grid height.</param>
    /// <param name="iterations">Number of water droplets to simulate.</param>
    /// <param name="seed">Deterministic seed for droplet placement.</param>
    public static void HydraulicErosion(float[] elevation, int width, int height,
                                         int iterations = 50000, uint seed = 42)
    {
        if (elevation == null) throw new ArgumentNullException(nameof(elevation));
        if (width < 2 || height < 2) throw new ArgumentException("Grid must be at least 2x2.");
        if (elevation.Length != width * height)
            throw new ArgumentException("Elevation array length must equal width * height.");

        uint rngState = seed;

        for (int i = 0; i < iterations; i++)
        {
            // Generate random starting position using xorshift32
            float posX = XorShiftNextFloat(ref rngState) * (width - 1);
            float posY = XorShiftNextFloat(ref rngState) * (height - 1);

            float dirX = 0f;
            float dirY = 0f;
            float speed = 1f;
            float water = 1f;
            float sediment = 0f;

            for (int lifetime = 0; lifetime < MaxDropletLifetime; lifetime++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;

                // Calculate droplet offset within the cell
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate height and gradient using bilinear interpolation
                int dropletIndex = nodeY * width + nodeX;

                // Get heights of the four corners of the cell
                float heightNW = elevation[dropletIndex];
                float heightNE = (nodeX + 1 < width) ? elevation[dropletIndex + 1] : heightNW;
                float heightSW = (nodeY + 1 < height) ? elevation[dropletIndex + width] : heightNW;
                float heightSE = (nodeX + 1 < width && nodeY + 1 < height)
                    ? elevation[dropletIndex + width + 1]
                    : heightNW;

                // Calculate gradient via bilinear interpolation
                float gradientX = (heightNE - heightNW) * (1 - cellOffsetY)
                                + (heightSE - heightSW) * cellOffsetY;
                float gradientY = (heightSW - heightNW) * (1 - cellOffsetX)
                                + (heightSE - heightNE) * cellOffsetX;

                // Update direction with inertia
                dirX = dirX * Inertia - gradientX * (1 - Inertia);
                dirY = dirY * Inertia - gradientY * (1 - Inertia);

                // Normalize direction
                float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
                if (len > 0.0001f)
                {
                    dirX /= len;
                    dirY /= len;
                }
                else
                {
                    // Random direction if flat
                    dirX = XorShiftNextFloat(ref rngState) * 2f - 1f;
                    dirY = XorShiftNextFloat(ref rngState) * 2f - 1f;
                    len = MathF.Sqrt(dirX * dirX + dirY * dirY);
                    if (len > 0.0001f)
                    {
                        dirX /= len;
                        dirY /= len;
                    }
                    else
                    {
                        break;
                    }
                }

                // Calculate new position
                float newPosX = posX + dirX;
                float newPosY = posY + dirY;

                // Check bounds - stop if droplet flows off the map
                if (newPosX < 0 || newPosX >= width - 1 || newPosY < 0 || newPosY >= height - 1)
                {
                    break;
                }

                // Calculate height difference
                float newHeight = GetInterpolatedHeight(elevation, width, height, newPosX, newPosY);
                float oldHeight = GetInterpolatedHeight(elevation, width, height, posX, posY);
                float deltaHeight = newHeight - oldHeight;

                // Calculate sediment capacity
                float sedimentCapacity = MathF.Max(
                    -deltaHeight * speed * water * SedimentCapacityFactor,
                    MinSedimentCapacity);

                // Deposit or erode
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // Deposit sediment
                    float amountToDeposit = (deltaHeight > 0)
                        ? MathF.Min(deltaHeight, sediment)
                        : (sediment - sedimentCapacity) * DepositSpeed;

                    sediment -= amountToDeposit;

                    // Deposit at the four corners of the current cell using bilinear weights
                    DepositSediment(elevation, width, height, nodeX, nodeY,
                                    cellOffsetX, cellOffsetY, amountToDeposit);
                }
                else
                {
                    // Erode terrain
                    float amountToErode = MathF.Min(
                        (sedimentCapacity - sediment) * ErodeSpeed,
                        -deltaHeight);

                    // Erode from the surrounding area (3x3 kernel centered on droplet)
                    ErodeTerrain(elevation, width, height, nodeX, nodeY,
                                 cellOffsetX, cellOffsetY, amountToErode);

                    sediment += amountToErode;
                }

                // Update speed and water
                speed = MathF.Sqrt(MathF.Max(0, speed * speed + deltaHeight * Gravity));
                water *= (1 - EvaporateSpeed);

                // Move droplet
                posX = newPosX;
                posY = newPosY;
            }
        }
    }

    /// <summary>
    /// Applies thermal erosion: material slides from steep slopes to adjacent
    /// lower cells until the talus angle threshold is satisfied.
    /// </summary>
    /// <param name="elevation">Mutable elevation array (width × height).</param>
    /// <param name="width">Grid width.</param>
    /// <param name="height">Grid height.</param>
    /// <param name="passes">Number of passes over the entire grid.</param>
    /// <param name="talusAngle">Maximum height difference threshold before material redistributes.</param>
    public static void ThermalErosion(float[] elevation, int width, int height,
                                       int passes = 5, float talusAngle = 0.6f)
    {
        if (elevation == null) throw new ArgumentNullException(nameof(elevation));
        if (width < 2 || height < 2) throw new ArgumentException("Grid must be at least 2x2.");
        if (elevation.Length != width * height)
            throw new ArgumentException("Elevation array length must equal width * height.");

        // 8-connected neighbor offsets (dx, dy)
        int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        for (int pass = 0; pass < passes; pass++)
        {
            // Process cells sequentially for determinism
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    float currentHeight = elevation[idx];

                    // Check all 8 neighbors
                    for (int n = 0; n < 8; n++)
                    {
                        int nx = x + dx[n];
                        int ny = y + dy[n];

                        // Skip out-of-bounds neighbors
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        int nIdx = ny * width + nx;
                        float neighborHeight = elevation[nIdx];
                        float heightDiff = currentHeight - neighborHeight;

                        // If height difference exceeds talus threshold, redistribute
                        if (heightDiff > talusAngle)
                        {
                            float amount = (heightDiff - talusAngle) * 0.5f;
                            elevation[idx] -= amount;
                            elevation[nIdx] += amount;

                            // Update current height for subsequent neighbor checks
                            currentHeight = elevation[idx];
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Deterministic xorshift32 random number generator.
    /// Returns the next uint in the sequence and advances the state.
    /// </summary>
    private static uint XorShift32(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    /// <summary>
    /// Returns a float in [0, 1) using xorshift32.
    /// </summary>
    private static float XorShiftNextFloat(ref uint state)
    {
        return (XorShift32(ref state) & 0x7FFFFFFF) / (float)0x80000000;
    }

    /// <summary>
    /// Gets the interpolated height at a continuous position using bilinear interpolation.
    /// </summary>
    private static float GetInterpolatedHeight(float[] elevation, int width, int height,
                                                float posX, float posY)
    {
        int x = (int)posX;
        int y = (int)posY;

        // Clamp to valid range
        x = Math.Clamp(x, 0, width - 2);
        y = Math.Clamp(y, 0, height - 2);

        float fx = posX - x;
        float fy = posY - y;

        int idx = y * width + x;

        float heightNW = elevation[idx];
        float heightNE = elevation[idx + 1];
        float heightSW = elevation[idx + width];
        float heightSE = elevation[idx + width + 1];

        // Bilinear interpolation
        float top = heightNW * (1 - fx) + heightNE * fx;
        float bottom = heightSW * (1 - fx) + heightSE * fx;
        return top * (1 - fy) + bottom * fy;
    }

    /// <summary>
    /// Deposits sediment at the four corners of the cell using bilinear weights.
    /// </summary>
    private static void DepositSediment(float[] elevation, int width, int height,
                                         int nodeX, int nodeY,
                                         float cellOffsetX, float cellOffsetY,
                                         float amount)
    {
        int idx = nodeY * width + nodeX;

        float w00 = (1 - cellOffsetX) * (1 - cellOffsetY);
        float w10 = cellOffsetX * (1 - cellOffsetY);
        float w01 = (1 - cellOffsetX) * cellOffsetY;
        float w11 = cellOffsetX * cellOffsetY;

        elevation[idx] += amount * w00;

        if (nodeX + 1 < width)
            elevation[idx + 1] += amount * w10;

        if (nodeY + 1 < height)
            elevation[idx + width] += amount * w01;

        if (nodeX + 1 < width && nodeY + 1 < height)
            elevation[idx + width + 1] += amount * w11;
    }

    /// <summary>
    /// Erodes terrain around the droplet position using a 3x3 kernel with distance-based weights.
    /// </summary>
    private static void ErodeTerrain(float[] elevation, int width, int height,
                                      int nodeX, int nodeY,
                                      float cellOffsetX, float cellOffsetY,
                                      float amount)
    {
        // Use bilinear weights for erosion at the four cell corners
        // This is simpler and more stable than a full 3x3 kernel
        int idx = nodeY * width + nodeX;

        float w00 = (1 - cellOffsetX) * (1 - cellOffsetY);
        float w10 = cellOffsetX * (1 - cellOffsetY);
        float w01 = (1 - cellOffsetX) * cellOffsetY;
        float w11 = cellOffsetX * cellOffsetY;

        elevation[idx] -= amount * w00;

        if (nodeX + 1 < width)
            elevation[idx + 1] -= amount * w10;

        if (nodeY + 1 < height)
            elevation[idx + width] -= amount * w01;

        if (nodeX + 1 < width && nodeY + 1 < height)
            elevation[idx + width + 1] -= amount * w11;
    }
}
