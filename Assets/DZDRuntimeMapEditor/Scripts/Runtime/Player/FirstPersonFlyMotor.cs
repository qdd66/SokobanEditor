using UnityEngine;

namespace DZDMapEditor
{
    public sealed class FirstPersonFlyMotor
    {
        public void Tick(
            Transform body,
            Transform lookPivot,
            FirstPersonFlyConfig config,
            in FlyInput input,
            float deltaTime,
            ref float pitch)
        {
            var yawDelta = input.LookDelta.x * config.LookSensitivity;
            var pitchDelta = input.LookDelta.y * config.LookSensitivity;
            if (!config.InvertY)
                pitchDelta = -pitchDelta;

            body.Rotate(Vector3.up, yawDelta, Space.World);
            pitch = Mathf.Clamp(pitch + pitchDelta, config.MinPitch, config.MaxPitch);
            lookPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            var planar = body.right * input.Planar.x + body.forward * input.Planar.y;
            var motion = planar * config.HorizontalSpeed
                         + Vector3.up * input.Vertical * config.VerticalSpeed;
            body.position += motion * deltaTime;
        }
    }
}
