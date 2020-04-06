using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Param {

/**
 * CombineMesh用のパラメータ
 */
[Serializable]
sealed class CombineMesh {
	public string dstMeshObjName = "Combined";		//!< 結合後のメッシュ名
}

}