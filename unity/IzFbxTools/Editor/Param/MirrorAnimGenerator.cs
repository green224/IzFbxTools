using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Param {

/**
 * MirrorAnimGenerator用のパラメータ
 */
[Serializable]
sealed class MirrorAnimGenerator {
	public bool shiftCycleOffset = true;	//!< ミラーリング時に開始時間を半分オフセットするか否か

	// ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス
	public string suffixL = "_L";
	public string suffixR = "_R";
}

}