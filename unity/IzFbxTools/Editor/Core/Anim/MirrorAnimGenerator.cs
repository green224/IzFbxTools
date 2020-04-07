using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core.Anim {

/**
 * 左右反転したアニメーションを生成する
 */
sealed class MirrorAnimGenerator {

	public string suffixL, suffixR;
	public bool shiftCycleOffset;		//!< 再生位置を半分シフトする。必ず右足から移動したい等の理由で使用する

	public MirrorAnimGenerator(
		string suffixL, string suffixR,
		bool shiftCycleOffset
	) {
		this.suffixL = suffixL;
		this.suffixR = suffixR;
		this.shiftCycleOffset = shiftCycleOffset;
	}

	public void proc( AnimationClip srcClip, AnimationClip dstClip ) {
		AnimCloner.clone( srcClip, dstClip );

		// 名前をミラー化
		dstClip.name = mirrorName(srcClip.name);

		// 再生位置を半分ずらす処理が必要な場合は、ここで行う
		if (shiftCycleOffset) {
			var dstAcs = AnimationUtility.GetAnimationClipSettings(dstClip);
			dstAcs.cycleOffset = (dstAcs.cycleOffset+0.5f) % 1;
			AnimationUtility.SetAnimationClipSettings(dstClip, dstAcs);
		}

		// カーブは変更か全消ししか無くて、ひとつずつ消すことが出来ない。
		// なのでとりあえず全消ししてからまたコピーする。
		dstClip.ClearCurves();

		// カーブをミラーリングする
		foreach (var i in AnimationUtility.GetCurveBindings( srcClip )) {
			var curve = AnimationUtility.GetEditorCurve( srcClip, i );
//			Debug.Log( "path:"+i.path);
//			Debug.Log( "propertyName:"+i.propertyName);
//			Debug.Log( "type:"+i.type);

			var newBnd = i;

			// パスを変換
			bool isLR = false;
			bool isCenter = false;
			var path = i.path;
			{
				if (path.EndsWith(suffixL)) {
					isLR = true;
					path = path.Substring(0,path.Length-2) + suffixR;
				} else if (path.EndsWith(suffixR)) {
					isLR = true;
					path = path.Substring(0,path.Length-2) + suffixL;
				}
				path = path.Replace( suffixL+"/", suffixR+"\n/" );
				path = path.Replace( suffixR+"/", suffixL+"\n/" );
				path = path.Replace( "\n", "" );
				isCenter = !isLR && !path.Contains(suffixL+"/") && !path.Contains(suffixL+"/");
			}
			newBnd.path = path;

			// キーをミラー
			var newCurve = new AnimationCurve();
			foreach ( var j in curve.keys){
				var newkey = j;
				if ( (
					// 位置をミラー
					(isLR||isCenter) && i.propertyName == "m_LocalPosition.x"
				) || (
					// 回転をミラー
					i.propertyName == "m_LocalRotation.y" || i.propertyName == "m_LocalRotation.z"
				) ) {
					newkey.value = -newkey.value;
					newkey.inTangent = -newkey.inTangent;
					newkey.outTangent = -newkey.outTangent;
				}
				newCurve.AddKey(newkey);
			}

			AnimationUtility.SetEditorCurve( dstClip, newBnd, newCurve );
		}

		// リファレンス系カーブはそのままコピー
		foreach (var i in AnimationUtility.GetObjectReferenceCurveBindings( srcClip )) {
			var keys = AnimationUtility.GetObjectReferenceCurve(srcClip, i);
			AnimationUtility.SetObjectReferenceCurve(dstClip, i, keys);
		}
	}

	/** 指定のアニメーション名の反転後名を取得 */
	public string mirrorName( string src ) {
		if (isEqualLastStr(src, suffixL)) return src.Substring(0,src.Length-1)+"R";
		if (isEqualLastStr(src, suffixR)) return src.Substring(0,src.Length-1)+"L";
		return src+"_Mirror";
	}

	/** 指定のアニメーションのファイルパスの反転後パスを取得 */
	public string mirrorFPath( AnimationClip src ) {
		var srcPath = AssetDatabase.GetAssetPath(src);

		var idx = srcPath.LastIndexOf("/");
		if (idx==-1) idx = srcPath.LastIndexOf("\\");

		if (idx==-1) {
			return mirrorName(src.name) + ".anim";
		} else {
			return srcPath.Substring(0,idx+1) + mirrorName(src.name) + ".anim";
		}
	}


	static bool isEqualLastStr(string tgt, string last) {
		return
			last.Length < tgt.Length &&
			tgt.Substring(tgt.Length-last.Length, last.Length) == last;
	}
}

}