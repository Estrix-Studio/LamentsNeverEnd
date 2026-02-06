using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
	[CreateAssetMenu(menuName = "Dialog", fileName = "New Dialog")]
	public class DialogInfo : ScriptableObject
	{
		public List<string> phrases;
	}
}