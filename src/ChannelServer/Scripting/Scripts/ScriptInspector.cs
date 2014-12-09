using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aura.Shared.Util;

namespace Aura.Channel.Scripting.Scripts
{
	public class ScriptInspector : IScript
	{
		public bool Init()
		{
			ChannelServer.Instance.ServerReady += Instance_ServerReady;
		}

		void Instance_ServerReady(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		public void Inspect()
		{
			Log.Info("ScriptInspector: Beginning inspection");
		}

		private void InspectNPCs()
		{
			var scripts = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(NpcScript).IsAssignableFrom(t) && !t.IsAbstract);

			foreach (var s in scripts)
			{
				var issues = new List<string>();
			}
		}
	}
}
