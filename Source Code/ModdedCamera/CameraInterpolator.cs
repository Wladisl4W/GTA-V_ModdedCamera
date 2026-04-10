using System;
using System.Collections.Generic;
using System.Diagnostics;
using GTA.Math;

namespace ModdedCamera
{
	/// <summary>
	/// Handles smooth camera interpolation between waypoints without using native spline functions.
	/// Implements Catmull-Rom spline interpolation for smooth curves.
	/// </summary>
	public class CameraInterpolator
	{
		private List<Vector3> _positions;
		private List<Vector3> _rotations;
		private List<int> _durations;

		private bool _isPlaying = false;
		private int _interpolationMode = 2; // 0 = Linear, 2 = Smooth (Catmull-Rom)

		// FIXED: Changed from static to instance-level to prevent race conditions
		// when multiple interpolators are created or during mod hot-reload
		private readonly Stopwatch _stopwatch = new Stopwatch();
		private double _playbackStartTimeMs = 0; // milliseconds with decimal precision

		// Cache total path duration
		private int _totalDurationMs = 0;

		public bool IsPlaying { get { return _isPlaying; } }
		public float PlaybackProgress { get; private set; }

		public int InterpolationMode
		{
			get { return _interpolationMode; }
			set { _interpolationMode = value; }
		}

		// Public method to adjust playback timing for dynamic speed changes
		public void SetPlaybackOffset(int elapsedMs)
		{
			_playbackStartTimeMs = _stopwatch.ElapsedMilliseconds - elapsedMs;
		}

		public CameraInterpolator()
		{
			_positions = new List<Vector3>();
			_rotations = new List<Vector3>();
			_durations = new List<int>();

			// Start stopwatch for this instance
			_stopwatch.Start();
		}

		public void SetPath(List<Vector3> positions, List<Vector3> rotations, List<int> durations)
		{
			try
			{
				if (positions == null || rotations == null || durations == null)
					throw new ArgumentNullException("Path data cannot be null");

				if (positions.Count < 2)
					throw new ArgumentException("Need at least 2 waypoints");

				if (positions.Count != rotations.Count || positions.Count != durations.Count)
					throw new ArgumentException("Position, rotation, and duration counts must match");

				_positions = new List<Vector3>(positions);
				_rotations = new List<Vector3>(rotations);
				_durations = new List<int>(durations);

				// Calculate total path duration
				_totalDurationMs = 0;
				foreach (int duration in _durations)
				{
					_totalDurationMs += Math.Max(10, duration); // Minimum 10ms per segment
				}

				Logger.Info("Path set with " + _positions.Count + " waypoints, total duration: " + _totalDurationMs + "ms");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "SetPath error");
				throw;
			}
		}

		public void Start()
		{
			try
			{
				if (_positions.Count < 2)
				{
					Logger.Warn("Cannot start playback - insufficient waypoints");
					return;
				}

				_isPlaying = true;
				_playbackStartTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
				PlaybackProgress = 0f;

				Logger.Info("Playback started - total duration: " + _totalDurationMs + "ms, instant looping enabled (high-precision timing)");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Start error");
			}
		}

		public void Stop()
		{
			_isPlaying = false;
			PlaybackProgress = 0f;
			Logger.Info("Playback stopped");
		}

		public void Update(out Vector3 position, out Vector3 rotation)
		{
			position = Vector3.Zero;
			rotation = Vector3.Zero;

			if (!_isPlaying || _positions.Count < 2 || _totalDurationMs <= 0)
				return;

			try
			{
				// Calculate elapsed time using high-precision stopwatch
				double elapsedMs = _stopwatch.Elapsed.TotalMilliseconds - _playbackStartTimeMs;

				// Handle negative elapsed (shouldn't happen with Stopwatch, but safety check)
				if (elapsedMs < 0)
				{
					Logger.Warn("Timing overflow detected, resetting playback");
					_playbackStartTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
					elapsedMs = 0;
				}

				// Use modulo for instant infinite looping - no delay
				double cycleTime = elapsedMs % _totalDurationMs;

				// FIXED: Explicit division by zero check (safety measure)
				if (_totalDurationMs == 0)
				{
					Logger.Warn("Update called with zero total duration - returning last position");
					position = _positions[_positions.Count - 1];
					rotation = _rotations[_rotations.Count - 1];
					return;
				}

				// Normal playback
				float progressRatio = (float)cycleTime / _totalDurationMs;
				PlaybackProgress = progressRatio;

				// Find which segment we're in
				double accumulatedMs = 0;
				int currentSegment = -1;

				for (int i = 0; i < _durations.Count - 1; i++)
				{
					int segmentDuration = Math.Max(10, _durations[i]);
					if (cycleTime < accumulatedMs + segmentDuration)
					{
						currentSegment = i;
						break;
					}
					accumulatedMs += segmentDuration;
				}

				// If we didn't find a segment, we're in the final dwell period
				// (last duration is treated as dwell at the final node). In that
				// case, return the last node's position/rotation instead of
				// attempting to interpolate across a non-existent segment.
				if (currentSegment == -1)
				{
					position = _positions[_positions.Count - 1];
					rotation = _rotations[_rotations.Count - 1];
					PlaybackProgress = 1f;
					return;
				}

				// Calculate t within current segment (0.0 to 1.0) with high precision
				int segmentDurationMs = Math.Max(10, _durations[currentSegment]);
				double segmentElapsedMs = cycleTime - accumulatedMs;
				float t = (float)segmentElapsedMs / segmentDurationMs;
				t = Math.Min(Math.Max(t, 0f), 1f); // Clamp to 0-1

				// Interpolate position
				if (_interpolationMode == 0)
				{
					// Linear mode: straight lines
					position = InterpolatePositionLinear(currentSegment, t);
				}
				else
				{
					// Smooth mode: Catmull-Rom
					position = InterpolatePositionSmooth(currentSegment, t);
				}

				// Interpolate rotation - BOTH modes use shortest path
				if (_interpolationMode == 0)
				{
					// Linear mode: linear interpolation with shortest path
					rotation = InterpolateRotationShortest(currentSegment, t);
				}
				else
				{
					// Smooth mode: interpolate with shortest path
					rotation = InterpolateRotationShortest(currentSegment, t);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Update error - continuing playback");
				// Don't stop playback on errors, just use last position
				position = _positions.Count > 0 ? _positions[_positions.Count - 1] : Vector3.Zero;
				rotation = _rotations.Count > 0 ? _rotations[_rotations.Count - 1] : Vector3.Zero;
			}
		}

		private Vector3 InterpolatePositionLinear(int segment, float t)
		{
			Vector3 p1 = _positions[segment];
			Vector3 p2 = _positions[segment + 1];
			return Vector3.Lerp(p1, p2, t);
		}

		private Vector3 InterpolatePositionSmooth(int segment, float t)
		{
			Vector3 p0 = segment == 0 ? _positions[0] : _positions[segment - 1];
			Vector3 p1 = _positions[segment];
			Vector3 p2 = _positions[segment + 1];
			Vector3 p3 = segment + 2 < _positions.Count ? _positions[segment + 2] : _positions[segment + 1];

			return CatmullRom(p0, p1, p2, p3, t);
		}

		private Vector3 InterpolateRotationShortest(int segment, float t)
		{
			Vector3 r1 = _rotations[segment];
			Vector3 r2 = _rotations[segment + 1];

			// Interpolate each component with shortest path for angles
			float x = LerpAngle(r1.X, r2.X, t);
			float y = LerpAngle(r1.Y, r2.Y, t);
			float z = LerpAngle(r1.Z, r2.Z, t);

			return new Vector3(x, y, z);
		}

		private float LerpAngle(float a, float b, float t)
		{
			// Calculate the shortest path difference
			float delta = b - a;

			// Normalize delta to [-180, 180] range
			while (delta > 180f) delta -= 360f;
			while (delta < -180f) delta += 360f;

			// Interpolate along shortest path
			return a + delta * t;
		}

		/// <summary>
		/// Catmull-Rom spline curve calculation
		/// </summary>
		private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			// Catmull-Rom basis matrix coefficients
			float t2 = t * t;
			float t3 = t2 * t;

			float v0 = -0.5f * t3 + t2 - 0.5f * t;
			float v1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
			float v2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
			float v3 = 0.5f * t3 - 0.5f * t2;

			return (v0 * p0) + (v1 * p1) + (v2 * p2) + (v3 * p3);
		}

		public void Clear()
		{
			_positions.Clear();
			_rotations.Clear();
			_durations.Clear();
			_isPlaying = false;
			_totalDurationMs = 0;
			PlaybackProgress = 0f;
		}
	}
}
