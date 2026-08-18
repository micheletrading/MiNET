using System.Collections.Generic;

namespace MiNET.Net
{
	public class Experiments : List<Experiments.Experiment>
	{
		/// <summary>Whether any experiments have ever been toggled in this world. Part of the wire type (trailing bool after the toggle list).</summary>
		public bool ExperimentsEverToggled { get; set; }

		public class Experiment
		{
			public string Name { get; }
			public bool Enabled { get; }

			public Experiment(string name, bool enabled)
			{
				Name = name;
				Enabled = enabled;
			}
		}
	}
}