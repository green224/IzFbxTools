using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core.Anim {

/**
 * Animationをコピーするための機能。
 */
static class AnimCloner {

	public static void clone( AnimationClip srcClip, AnimationClip dstClip ) {

		dstClip.wrapMode = srcClip.wrapMode;
		dstClip.frameRate = srcClip.frameRate;
		var srcAcs = AnimationUtility.GetAnimationClipSettings(srcClip);
		var dstAcs = AnimationUtility.GetAnimationClipSettings(dstClip);
		dstAcs.cycleOffset = srcAcs.cycleOffset;
		dstAcs.heightFromFeet = srcAcs.heightFromFeet;
		dstAcs.keepOriginalOrientation = srcAcs.keepOriginalOrientation;
		dstAcs.keepOriginalPositionXZ = srcAcs.keepOriginalPositionXZ;
		dstAcs.keepOriginalPositionY = srcAcs.keepOriginalPositionY;
		dstAcs.level = srcAcs.level;
		dstAcs.loopBlend = srcAcs.loopBlend;
		dstAcs.loopBlendOrientation = srcAcs.loopBlendOrientation;
		dstAcs.loopBlendPositionXZ = srcAcs.loopBlendPositionXZ;
		dstAcs.loopBlendPositionY = srcAcs.loopBlendPositionY;
		dstAcs.loopTime = srcAcs.loopTime;
		dstAcs.mirror = srcAcs.mirror;
		dstAcs.orientationOffsetY = srcAcs.orientationOffsetY;
		dstAcs.startTime = srcAcs.startTime;
		dstAcs.stopTime = srcAcs.stopTime;
		dstAcs.additiveReferencePoseClip = srcAcs.additiveReferencePoseClip;
		dstAcs.additiveReferencePoseTime = srcAcs.additiveReferencePoseTime;
		dstAcs.hasAdditiveReferencePose = srcAcs.hasAdditiveReferencePose;
		AnimationUtility.SetAnimationClipSettings(dstClip, dstAcs);

		dstClip.ClearCurves();

		foreach (var i in AnimationUtility.GetCurveBindings( srcClip )) {
			var ac = AnimationUtility.GetEditorCurve(srcClip, i);
			AnimationUtility.SetEditorCurve(dstClip, i, ac);
		}

		foreach (var i in AnimationUtility.GetObjectReferenceCurveBindings( srcClip )) {
			var keys = AnimationUtility.GetObjectReferenceCurve(srcClip, i);
			AnimationUtility.SetObjectReferenceCurve(dstClip, i, keys);
		}
	}

}

}