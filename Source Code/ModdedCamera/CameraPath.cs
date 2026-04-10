using System;
using System.Collections.Generic;
using GTA.Math;

namespace ModdedCamera
{
    [Serializable]
    public class CameraPath
    {
        public string Name { get; set; }
        public List<Vector3> Positions { get; set; }
        public List<Vector3> Rotations { get; set; }
        public List<int> Durations { get; set; }
        public int DefaultDuration { get; set; }
        public int Fov { get; set; }
        public int Speed { get; set; }
        public int InterpolationMode { get; set; }

        public CameraPath()
        {
            this.Positions = new List<Vector3>();
            this.Rotations = new List<Vector3>();
            this.Durations = new List<int>();
            this.DefaultDuration = 5000;
            this.Fov = 50;
            this.Speed = 3;
            this.InterpolationMode = 2; // 2 = Smooth, 0 = Linear
        }

        public CameraPath(string name, List<Tuple<Vector3, Vector3>> nodes, int defaultDuration, int fov, int speed, int interpolationMode)
        {
            if (nodes == null) throw new ArgumentNullException("nodes", "Node list cannot be null");
            this.Name = name;
            this.Positions = new List<Vector3>();
            this.Rotations = new List<Vector3>();
            this.Durations = new List<int>();
            this.DefaultDuration = defaultDuration;
            this.Fov = fov;
            this.Speed = speed;
            this.InterpolationMode = interpolationMode;

            foreach (var node in nodes)
            {
                this.Positions.Add(node.Item1);
                this.Rotations.Add(node.Item2);
                this.Durations.Add(defaultDuration);
            }
        }

        // Constructor that preserves individual node durations
        public CameraPath(string name, List<Vector3> positions, List<Vector3> rotations, List<int> durations, int defaultDuration, int fov, int speed, int interpolationMode)
        {
            this.Name = name;
            this.Positions = positions ?? new List<Vector3>();
            this.Rotations = rotations ?? new List<Vector3>();
            this.Durations = durations ?? new List<int>();
            this.DefaultDuration = defaultDuration;
            this.Fov = fov;
            this.Speed = speed;
            this.InterpolationMode = interpolationMode;
        }

        public List<Tuple<Vector3, Vector3>> ToNodes()
        {
            var nodes = new List<Tuple<Vector3, Vector3>>();
            int count = Math.Min(this.Positions.Count, this.Rotations.Count);
            for (int i = 0; i < count; i++)
            {
                nodes.Add(new Tuple<Vector3, Vector3>(this.Positions[i], this.Rotations[i]));
            }
            return nodes;
        }
    }
}
