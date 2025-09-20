using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenMetaverse;

namespace CrystalFrost.Assets.Animation
{

	/// <summary>
	/// Represents the possible hand poses.
	/// </summary>
	public enum EHandPose
	{
		/// <summary>
		/// Spread hand pose.
		/// </summary>
		HandPoseSpread = 0,
		/// <summary>
		/// Relaxed hand pose.
		/// </summary>
		HandPoseRelaxed,
		/// <summary>
		/// Pointing hand pose.
		/// </summary>
		HandPosePoint,
		/// <summary>
		/// Fist hand pose.
		/// </summary>
		HandPoseFist,
		/// <summary>
		/// Relaxed left hand pose.
		/// </summary>
		HandPoseRelaxedL,
		/// <summary>
		/// Pointing left hand pose.
		/// </summary>
		HandPosePointL,
		/// <summary>
		/// Fist left hand pose.
		/// </summary>
		HandPoseFistL,
		/// <summary>
		/// Relaxed right hand pose.
		/// </summary>
		HandPoseRelaxedR,
		/// <summary>
		/// Pointing right hand pose.
		/// </summary>
		HandPosePointR,
		/// <summary>
		/// Fist right hand pose.
		/// </summary>
		HandPoseFistR,
		/// <summary>
		/// Salute right hand pose.
		/// </summary>
		HandPoseSaluteR,
		/// <summary>
		/// Typing hand pose.
		/// </summary>
		HandPoseTyping,
		/// <summary>
		/// Peace right hand pose.
		/// </summary>
		HandPosePeaceR,
		/// <summary>
		/// Palm right hand pose.
		/// </summary>
		HandPosePalmR,
		/// <summary>
		/// The number of hand poses.
		/// </summary>
		NumHandPoses
	}

	/// <summary>
	/// Provides extension methods for <see cref="EHandPose"/>.
	/// </summary>
	public static class EHandPoseExtensions
	{
		/// <summary>
		/// Converts a <see cref="uint"/> to an <see cref="EHandPose"/>.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted <see cref="EHandPose"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not a valid hand pose.</exception>
		public static EHandPose FromUInt(uint value)
		{
			if (value >= (uint)EHandPose.NumHandPoses)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Invalid hand pose value.");
			}

			return (EHandPose)value;
		}
	}

	/// <summary>
	/// Represents the header of an animation.
	/// </summary>
	public struct AnimationHeader
	{
		/// <summary>
		/// The version of the animation format.
		/// </summary>
		public ushort Version { get; set; }
		/// <summary>
		/// The sub-version of the animation format.
		/// </summary>
		public ushort SubVersion { get; set; }
		/// <summary>
		/// The base priority of the animation.
		/// </summary>
		public int BasePriority { get; set; }
		/// <summary>
		/// The duration of the animation in seconds.
		/// </summary>
		public float Duration { get; set; }
		/// <summary>
		/// The name of the emote.
		/// </summary>
		public string EmoteName { get; set; }
		/// <summary>
		/// The loop in point of the animation.
		/// </summary>
		public float LoopInPoint { get; set; }
		/// <summary>
		/// The loop out point of the animation.
		/// </summary>
		public float LoopOutPoint { get; set; }
		/// <summary>
		/// Whether the animation should loop.
		/// </summary>
		public int Loop { get; set; }
		/// <summary>
		/// The duration of the ease in.
		/// </summary>
		public float EaseInDuration { get; set; }
		/// <summary>
		/// The duration of the ease out.
		/// </summary>
		public float EaseOutDuration { get; set; }
		/// <summary>
		/// The hand pose of the animation.
		/// </summary>
		public EHandPose HandPose { get; set; }
		/// <summary>
		/// The number of joints in the animation.
		/// </summary>
		public uint NumJoints { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"Version: {Version}, SubVersion: {SubVersion}, BasePriority: {BasePriority}, Duration: {Duration}, EmoteName: {EmoteName}, LoopInPoint: {LoopInPoint}, LoopOutPoint: {LoopOutPoint}, Loop: {Loop}, EaseInDuration: {EaseInDuration}, EaseOutDuration: {EaseOutDuration}, HandPose: {HandPose}, NumJoints: {NumJoints}";
		}
	}

	/// <summary>
	/// Represents a keyframe in an animation.
	/// </summary>
	public struct AnimationKeyframe
	{
		/// <summary>
		/// The time of the keyframe.
		/// </summary>
		public ushort Time { get; set; }
		/// <summary>
		/// The X component of the keyframe.
		/// </summary>
		public ushort X { get; set; }
		/// <summary>
		/// The Y component of the keyframe.
		/// </summary>
		public ushort Y { get; set; }
		/// <summary>
		/// The Z component of the keyframe.
		/// </summary>
		public ushort Z { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"Time: {Time}, X: {X}, Y: {Y}, Z: {Z}";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationKeyframe"/> struct.
		/// </summary>
		/// <param name="time">The time of the keyframe.</param>
		/// <param name="x">The X component of the keyframe.</param>
		/// <param name="y">The Y component of the keyframe.</param>
		/// <param name="z">The Z component of the keyframe.</param>
		public AnimationKeyframe(ushort time, ushort x, ushort y, ushort z)
		{
			Time = time;
			X = x;
			Y = y;
			Z = z;
		}
	}

	/// <summary>
	/// Represents the data for a joint in an animation.
	/// </summary>
	public struct AnimationJointData
	{
		/// <summary>
		/// The name of the joint.
		/// </summary>
		public string JointName { get; set; }
		/// <summary>
		/// The priority of the joint.
		/// </summary>
		public int JointPriority { get; set; }

		/// <summary>
		/// The rotation keyframes for the joint.
		/// </summary>
		public AnimationKeyframe[] RotationKeys { get; set; }
		/// <summary>
		/// The position keyframes for the joint.
		/// </summary>
		public AnimationKeyframe[] PositionKeys { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"JointName: {JointName}, JointPriority: {JointPriority}, RotationKeys: {RotationKeys.Length}, PositionKeys: {PositionKeys.Length}";
		}
	}

	/// <summary>
	/// Represents a constraint in an animation.
	/// </summary>
	public struct AnimationConstraint
	{
		/// <summary>
		/// The length of the chain.
		/// </summary>
		public byte ChainLength { get; set; }  // U8
		/// <summary>
		/// The type of the constraint.
		/// </summary>
		public byte ConstraintType { get; set; }  // U8 (0: point*, 1: plane)
		/// <summary>
		/// The source volume of the constraint.
		/// </summary>
		public string SourceVolume { get; set; }  // char[16]
		/// <summary>
		/// The source offset of the constraint.
		/// </summary>
		public OpenMetaverse.Vector3 SourceOffset { get; set; }
		/// <summary>
		/// The target volume of the constraint.
		/// </summary>
		public string TargetVolume { get; set; }  // char[16]
		/// <summary>
		/// The target offset of the constraint.
		/// </summary>
		public OpenMetaverse.Vector3 TargetOffset { get; set; }
		/// <summary>
		/// The target direction of the constraint.
		/// </summary>
		public OpenMetaverse.Vector3 TargetDir { get; set; }
		/// <summary>
		/// The start of the ease in.
		/// </summary>
		public float EaseInStart { get; set; }  // F32
		/// <summary>
		/// The stop of the ease in.
		/// </summary>
		public float EaseInStop { get; set; }  // F32
		/// <summary>
		/// The start of the ease out.
		/// </summary>
		public float EaseOutStart { get; set; }  // F32
		/// <summary>
		/// The stop of the ease out.
		/// </summary>
		public float EaseOutStop { get; set; }  // F32

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"ChainLength: {ChainLength}, ConstraintType: {ConstraintType}, SourceVolume: {SourceVolume}, SourceOffset: {SourceOffset}, TargetVolume: {TargetVolume}, TargetOffset: {TargetOffset}, TargetDir: {TargetDir}, EaseInStart: {EaseInStart}, EaseInStop: {EaseInStop}, EaseOutStart: {EaseOutStart}, EaseOutStop: {EaseOutStop}";
		}
	}

	/// <summary>
	/// Represents a decoded animation.
	/// </summary>
	public class DecodedAnimation
	{
		/// <summary>
		/// The UUID of the animation.
		/// </summary>
		public UUID animationId;
		/// <summary>
		/// The header of the animation.
		/// </summary>
		public AnimationHeader Header;
		/// <summary>
		/// The joint data of the animation.
		/// </summary>
		public AnimationJointData[] JointData;
		/// <summary>
		/// The constraints of the animation.
		/// </summary>
		public AnimationConstraint[] Constraints;
	}
}

