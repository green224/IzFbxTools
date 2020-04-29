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
sealed class VisibilityAnimGenerator {

	public string regexVisBonePtn;			//!< 対象ボーン特定用正規表現

	/**
	 * まとめて表示状態を変更するボーンがある場合に、
	 * アニメーション操作対象自体を1つにまとめてしまう場合の、
	 * まとめた結果のオブジェクト名を取得する処理。
	 * まとめる必要が無い場合は、nullを指定しておけばよい。
	 */
	public Func<string> getCombinedName = null;

	/**
	 * アニメーション適応先のレンダラーオブジェクト名が分かっている際は、
	 * ここにその名前を指定しておく事ができる。
	 * Visibilityターゲット名の前方・後方一致機能を使用する場合は、
	 * ここに情報をちゃんと設定しておくこと。でないと部分一致機能は使用できない。
	 */
	public List<string> visTargetableNames = null;

	/**
	 * 表示状態を切り替える対象のオブジェクト情報
	 * 処理を行った結果ログ。参照専用。
	 */
	public sealed class VisTargetObjInfo {
		/**
		 * 表示状態変更対象のオブジェクト名。
		 * これが1つの場合はそのままアニメーション対象として処理されるが、
		 * 複数ある場合は一つに纏めて出力される可能性がある。
		 */
		public List<string> srcObjNames = new List<string>();

		/**
		 * 表示状態変更対象のオブジェクトが複数ある場合に、
		 * アニメーション対象を1つにまとめてしまう場合の、
		 * まとめた結果のオブジェクト名。
		 * まとめる必要が愛場合はnullとなる。
		 */
		public string combinedTgtName = null;

		/** その表示状態変更対象のオブジェクト名が、部分一致フォーマットであるか否か */
		static public bool isPartialMatchFmt(string srcObjName) => srcObjName.Length!=0 && (srcObjName[0]=='*' || srcObjName.Last()=='*');

		/** 表示状態変更対象のオブジェクト名の、部分一致判定 */
		static public bool isPartialMatch(string srcObjName, string tgtName) {
			if (srcObjName[0]=='*') return tgtName.EndsWith(srcObjName.Substring(1));
			if (srcObjName.Last()=='*') return tgtName.StartsWith(srcObjName.Substring(0,srcObjName.Length-1));
			return srcObjName == tgtName;
		}
	}
	public readonly Dictionary<string, VisTargetObjInfo> visTargetObjInfos = new Dictionary<string, VisTargetObjInfo>();

	public VisibilityAnimGenerator( Param.VisibilityAnimGenerator param ) {
		regexVisBonePtn = param.regexPattern;
	}

	public void proc(AnimationClip srcClip, AnimationClip dstClip) {
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
				continue;
			}

			// 対象ボーンの m_LocalScale.x をフラグとする
			if (i.propertyName != "m_LocalScale.x") continue;

			// このVis対象に対しての処理が初めてか否かの判定
			var m = rgxVisBone.Match(i.path);
			var caps = m.Groups[1].Captures;
			VisTargetObjInfo vtoi;
			bool is1stProcBone;
			if (is1stProcBone = !visTargetObjInfos.TryGetValue(m.Value, out vtoi)) {
				vtoi = new VisTargetObjInfo();
				foreach (System.Text.RegularExpressions.Capture j in caps) {
					if (VisTargetObjInfo.isPartialMatchFmt( j.Value )) {
						// Vis操作対象オブジェクト名に部分一致フォーマットが指定されている
						if (visTargetableNames == null) Debug.LogError("visTargetableNamesが設定されていません");
						else foreach (var k in visTargetableNames)
							if (VisTargetObjInfo.isPartialMatch(j.Value, k)) vtoi.srcObjNames.Add(k);
					} else {
						vtoi.srcObjNames.Add(j.Value);
					}
				}
				visTargetObjInfos[m.Value] = vtoi;
			}

			// 指定されたVisibility変更ターゲットで、カーブを生成する
			Action<string> makeCurve = path => {
				var binding = new EditorCurveBinding();
				binding.path = path;
				binding.type = typeof(SkinnedMeshRenderer);
				binding.propertyName = "m_Enabled";
				AnimationUtility.SetEditorCurve(dstClip, binding, ac);
			};
			if (getCombinedName == null || caps.Count == 1) {
				foreach (var j in vtoi.srcObjNames) makeCurve( j );
			} else {
				if (is1stProcBone) vtoi.combinedTgtName = getCombinedName();
				makeCurve( vtoi.combinedTgtName );
			}
		}

		// リファレンス系カーブはそのままコピー
		foreach (var i in AnimationUtility.GetObjectReferenceCurveBindings( srcClip )) {
			var keys = AnimationUtility.GetObjectReferenceCurve(srcClip, i);
			AnimationUtility.SetObjectReferenceCurve(dstClip, i, keys);
		}

		// ログの生成
		var log = Log.instance;
		log.endVisAnimGeneration( true, srcClip );
	}

}

}