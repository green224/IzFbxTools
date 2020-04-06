using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Param {

/**
 * EdgeMerge用のパラメータ
 */
[Serializable]
sealed class EdgeMerge {
	public float mergeLength = 0.0001f;		//!< マージ距離
}

}