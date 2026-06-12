using System;
using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// Provides Catmull-Rom bicubic interpolation for terrain elevation at subdivision
/// vertices. Supports configurable subdivision factors (1×–8×) per quality tier.
///
/// Catmull-Rom splines guarantee C1 continuity across cell and chunk boundaries,
/// eliminating seams, cracks, and T-junctions at any subdivision level.
///
/// At factor 1× (Potato), results are identical to the current TerrainRenderer
/// (sampling at integer grid positions only).
/// </summary>
public sealed class TerrainSubdivision
{
    private float[]? _baseElevation;
    private int _width;
    private int _height;

    /// <summary>Number of sub-vertices per grid cell edge.</summary>
    public int SubdivisionFactor { get; }

    /// <summary>
    /// Creates a TerrainSubdivision configured for the given quality tier.
    /// </summary>
    public TerrainSubdivision(QualityTier tier)
    {
        SubdivisionFactor = tier switch
        {
            QualityTier.Potato => 1,
            QualityTier.Low => 2,
            QualityTier.Medium => 4,
            QualityTier.High => 8,
            _ => 1
        };
    }

    /// <summary>
    /// Creates a TerrainSubdivision with an explicit subdivision factor.
    /// </summary>
    public TerrainSubdivision(int subdivisionFactor)
    {
        SubdivisionFactor = Math.Max(1, subdivisionFactor);
    }

    /// <summary>
    /// Sets the base elevation data that interpolation operates on.
    /// </summary>
    /// <param name="baseElevation">Flat array of elevation values (row-major, width × height).</param>
    /// <param name="width">Grid width in cells.</param>
    /// <param name="height">Grid height in cells.</param>
    public void SetElevationData(float[] baseElevation, int width, int height)
    {
        _baseElevation = baseElevation;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Returns the interpolated elevation at a fractional grid position using
    /// Catmull-Rom bicubic interpolation.
    ///
    /// This is the same interpolation used for mesh vertex generation, ensuring
    /// that units querying elevation sit exactly on the rendered surface.
    ///
    /// For integer positions, this returns the exact base elevation value.
    /// </summary>
    /// <param name="baseElevation">Flat elevation array (row-major).</param>
    /// <param name="width">Grid width.</param>
    /// <param name="height">Grid height.</param>
    /// <param name="fx">Fractional X position in grid space.</param>
    /// <param name="fy">Fractional Y position in grid space.</param>
    /// <returns>Interpolated elevation value.</returns>
    public static float SampleElevation(float[] baseElevation, int width, int height, float fx, float fy)
    {
        // Determine the integer cell and fractional offset
        int ix = (int)MathF.Floor(fx);
        int iy = (int)MathF.Floor(fy);
        float tx = fx - ix;
        float ty = fy - iy;

        // Catmull-Rom uses 4 control points: P(i-1), P(i), P(i+1), P(i+2)
        // For bicubic: interpolate 4 rows, then interpolate the 4 results

        // Sample 4×4 grid of elevation values with clamped edge access
        float row0 = CatmullRom1D(
            SampleClamped(baseElevation, width, height, ix - 1, iy - 1),
            SampleClamped(baseElevation, width, height, ix, iy - 1),
            SampleClamped(baseElevation, width, height, ix + 1, iy - 1),
            SampleClamped(baseElevation, width, height, ix + 2, iy - 1),
            tx);

        float row1 = CatmullRom1D(
            SampleClamped(baseElevation, width, height, ix - 1, iy),
            SampleClamped(baseElevation, width, height, ix, iy),
            SampleClamped(baseElevation, width, height, ix + 1, iy),
            SampleClamped(baseElevation, width, height, ix + 2, iy),
            tx);

        float row2 = CatmullRom1D(
            SampleClamped(baseElevation, width, height, ix - 1, iy + 1),
            SampleClamped(baseElevation, width, height, ix, iy + 1),
            SampleClamped(baseElevation, width, height, ix + 1, iy + 1),
            SampleClamped(baseElevation, width, height, ix + 2, iy + 1),
            tx);

        float row3 = CatmullRom1D(
            SampleClamped(baseElevation, width, height, ix - 1, iy + 2),
            SampleClamped(baseElevation, width, height, ix, iy + 2),
            SampleClamped(baseElevation, width, height, ix + 1, iy + 2),
            SampleClamped(baseElevation, width, height, ix + 2, iy + 2),
            tx);

        // Interpolate the 4 row results vertically
        return CatmullRom1D(row0, row1, row2, row3, ty);
    }

    /// <summary>
    /// Computes a smooth normal vector at a fractional grid position using
    /// Sobel-filtered elevation samples around the point.
    ///
    /// Uses a 3×3 Sobel kernel with Catmull-Rom sampled elevations for
    /// sub-cell accuracy.
    /// </summary>
    /// <param name="baseElevation">Flat elevation array (row-major).</param>
    /// <param name="width">Grid width.</param>
    /// <param name="height">Grid height.</param>
    /// <param name="fx">Fractional X position in grid space.</param>
    /// <param name="fy">Fractional Y position in grid space.</param>
    /// <returns>Normalized surface normal vector.</returns>
    public static Vector3 ComputeSobelNormal(float[] baseElevation, int width, int height, float fx, float fy)
    {
        // Sample a 3×3 grid of elevations around the point using Catmull-Rom
        // The step size is 1 grid unit (matches the base grid spacing)
        const float step = 1.0f;

        float e00 = SampleElevation(baseElevation, width, height, fx - step, fy - step);
        float e10 = SampleElevation(baseElevation, width, height, fx, fy - step);
        float e20 = SampleElevation(baseElevation, width, height, fx + step, fy - step);

        float e01 = SampleElevation(baseElevation, width, height, fx - step, fy);
        // e11 = center, not needed for Sobel
        float e21 = SampleElevation(baseElevation, width, height, fx + step, fy);

        float e02 = SampleElevation(baseElevation, width, height, fx - step, fy + step);
        float e12 = SampleElevation(baseElevation, width, height, fx, fy + step);
        float e22 = SampleElevation(baseElevation, width, height, fx + step, fy + step);

        // Sobel 3×3 kernel for X gradient:
        //  -1  0  1
        //  -2  0  2
        //  -1  0  1
        float dzdx = (e20 + 2.0f * e21 + e22) - (e00 + 2.0f * e01 + e02);

        // Sobel 3×3 kernel for Y gradient:
        //  -1 -2 -1
        //   0  0  0
        //   1  2  1
        float dzdy = (e02 + 2.0f * e12 + e22) - (e00 + 2.0f * e10 + e20);

        // Normal from gradient: N = (-dz/dx, scale, -dz/dy) normalized
        // Scale factor of 8 accounts for the Sobel kernel weighting (sum of weights = 8)
        var normal = new Vector3(-dzdx, 8.0f, -dzdy);
        return normal.Normalized();
    }

    /// <summary>
    /// Convenience wrapper that returns interpolated elevation at a world position.
    /// World coordinates map directly to grid coordinates (1 unit = 1 cell).
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Interpolated elevation, or 0 if no elevation data is set.</returns>
    public float GetElevationAtWorld(float worldX, float worldZ)
    {
        if (_baseElevation == null) return 0f;
        return SampleElevation(_baseElevation, _width, _height, worldX, worldZ);
    }

    /// <summary>
    /// Evaluates the Catmull-Rom spline for 4 control points at parameter t.
    ///
    /// The Catmull-Rom spline passes through P1 at t=0 and P2 at t=1,
    /// using P0 and P3 to determine tangent directions. This guarantees
    /// C1 continuity at knot points (cell boundaries).
    ///
    /// Formula: 0.5 * ((2*P1) + (-P0 + P2)*t + (2*P0 - 5*P1 + 4*P2 - P3)*t² + (-P0 + 3*P1 - 3*P2 + P3)*t³)
    /// </summary>
    private static float CatmullRom1D(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2.0f * p1) +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
        );
    }

    /// <summary>
    /// Samples the elevation map with clamped boundary access.
    /// When coordinates are out of bounds, the nearest valid sample is used.
    /// This ensures C1 continuity at grid edges (no discontinuities at map borders).
    /// </summary>
    private static float SampleClamped(float[] elevation, int width, int height, int x, int y)
    {
        int cx = Math.Clamp(x, 0, width - 1);
        int cy = Math.Clamp(y, 0, height - 1);
        return elevation[cy * width + cx];
    }
}
