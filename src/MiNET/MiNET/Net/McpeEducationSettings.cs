#region LICENSE

// The contents of this file are subject to the Common Public Attribution
// License Version 1.0. (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE.
// The License is based on the Mozilla Public License Version 1.1, but Sections 14
// and 15 have been added to cover use of software over a computer network and
// provide for limited attribution for the Original Developer. In addition, Exhibit A has
// been modified to be consistent with Exhibit B.
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
// the specific language governing rights and limitations under the License.
//
// The Original Code is MiNET.
//
// The Original Developer is the Initial Developer.  The Initial Developer of
// the Original Code is Niclas Olofsson.
//
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.
// All Rights Reserved.

#endregion

namespace MiNET.Net
{
	public partial class McpeEducationSettings : Packet<McpeEducationSettings>
	{
		public bool HasAgentCapabilities;
		public bool HasAgentCapabilitiesCanModifyBlocks;
		public bool AgentCapabilitiesCanModifyBlocks;
		public string CodeBuilderOverrideUri; // null when absent
		public bool HasQuiz;
		public bool HasLinkSettings;
		public string LinkSettingsUrl;
		public string LinkSettingsDisplayName;

		partial void AfterEncode()
		{
			Write(HasAgentCapabilities);
			if (HasAgentCapabilities)
			{
				Write(HasAgentCapabilitiesCanModifyBlocks);
				if (HasAgentCapabilitiesCanModifyBlocks) Write(AgentCapabilitiesCanModifyBlocks);
			}

			Write(CodeBuilderOverrideUri != null);
			if (CodeBuilderOverrideUri != null) Write(CodeBuilderOverrideUri);

			Write(HasQuiz);

			Write(HasLinkSettings);
			if (HasLinkSettings)
			{
				Write(LinkSettingsUrl);
				Write(LinkSettingsDisplayName);
			}
		}

		partial void AfterDecode()
		{
			HasAgentCapabilities = ReadBool();
			if (HasAgentCapabilities)
			{
				HasAgentCapabilitiesCanModifyBlocks = ReadBool();
				if (HasAgentCapabilitiesCanModifyBlocks) AgentCapabilitiesCanModifyBlocks = ReadBool();
			}

			if (ReadBool()) CodeBuilderOverrideUri = ReadString();

			HasQuiz = ReadBool();

			HasLinkSettings = ReadBool();
			if (HasLinkSettings)
			{
				LinkSettingsUrl = ReadString();
				LinkSettingsDisplayName = ReadString();
			}
		}
	}
}
