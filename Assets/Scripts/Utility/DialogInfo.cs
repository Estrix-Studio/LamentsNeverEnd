using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utility
{
	[CreateAssetMenu(menuName = "Dialog", fileName = "New Dialog")]
	public class DialogInfo : ScriptableObject
	{
		[SerializeField] private TextAsset yarnScript;
		[SerializeField] private string yarnNodeName = "Start";

		[FormerlySerializedAs("Phrases")]
		public List<string> phrases;

		public TextAsset YarnScript => yarnScript;
		public string YarnNodeName => string.IsNullOrWhiteSpace(yarnNodeName) ? "Start" : yarnNodeName;
		public bool HasYarnScript => yarnScript != null;
	}
}
