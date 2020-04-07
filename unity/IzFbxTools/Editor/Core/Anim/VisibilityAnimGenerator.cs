using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core.Anim {

/**
 * ボーンアニメーションをフラグと見立てて、Visibilityのアニメーションへ変換する処理。
 *
 * BlenderなどVisibilityアニメーション付きのFBXを生成できないモデラーから、
 * Visibilityアニメーションを出力したいとき、一旦ボーンアニメーションとして出力してから
 * それをVisibilityアニメーションに変換するという手がある。
 * これはボーンのスケール値アニメーションをフラグとみたてて、
 * Visibilityアニメーションを構築する。
 *
 * ボーン名に "[vis:オブジェクト名]" という文字列を含ませておくこと。
 * 指定されたオブジェクト名のものについているSkinnedMeshRendererのEnableが
 * 切り替わるアニメーションが生成される。
 */
static class VisibilityAnimGenerator {

	public static void proc(
		AnimationClip srcClip, AnimationClip dstClip,
		string regexVisBonePtn		//!< 対象ボーン特定用正規表現
	) {
		AnimCloner.clone( srcClip, dstClip );

		// 対象ボーン特定用正規表現
		var rgxVisBone = new System.Text.RegularExpressions.Regex(regexVisBonePtn);


		// カーブは変更か全消ししか無くて、ひとつずつ消すことが出来ない。
		// なのでとりあえず全消ししてからまたコピーする。
		dstClip.ClearCurves();

		foreach (var i in AnimationUtility.GetCurveBindings( srcClip )) {
			var ac = AnimationUtility.GetEditorCurve(srcClip, i);
			if ( !rgxVisBone.IsMatch(i.path) ) {
				// 対象外ボーンはそのままコピー
				AnimationUtility.SetEditorCurve(dstClip, i, ac);
			}

			// 対象ボーンの m_LocalScale.x をフラグとする
			if (i.propertyName != "m_LocalScale.x") continue;

			// 指定されたVisibility変更ターゲットで、カーブを生成する
			foreach (System.Text.RegularExpressions.Match j in rgxVisBone.Matches(i.path)) {
				var binding = new EditorCurveBinding();
				binding.path = j.Groups[1].Value;
				binding.type = typeof(SkinnedMeshRenderer);
				binding.propertyName = "m_Enabled";
				AnimationUtility.SetEditorCurve(dstClip, binding, ac);
			}
		}

		// リファレンス系カーブはそのままコピー
		foreach (var i in AnimationUtility.GetObjectReferenceCurveBindings( srcClip )) {
			var keys = AnimationUtility.GetObjectReferenceCurve(srcClip, i);
			AnimationUtility.SetObjectReferenceCurve(dstClip, i, keys);
		}
	}

}

}