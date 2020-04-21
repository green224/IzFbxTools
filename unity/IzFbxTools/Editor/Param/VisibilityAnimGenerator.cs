using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Param {

/**
 * VisibilityAnimGenerator用のパラメータ
 */
[Serializable]
sealed class VisibilityAnimGenerator {
	public string regexPattern = @"\[vis:(?:(?:,?)([\w\-\\]+))+\]";		//!< 対象ボーン名判定パターン
}

}