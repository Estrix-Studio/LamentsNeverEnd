using UnityEngine;

namespace EndlessSeamlessLevels
{
	public enum LevelValidationSeverity
	{
		Info,
		Warning,
		Error
	}

	public readonly struct LevelValidationMessage
	{
		public LevelValidationMessage(LevelValidationSeverity severity, string message, Object context)
		{
			Severity = severity;
			Message = message;
			Context = context;
		}

		public LevelValidationSeverity Severity { get; }
		public string Message { get; }
		public Object Context { get; }
	}
}
