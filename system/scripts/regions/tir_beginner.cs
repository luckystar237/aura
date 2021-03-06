//--- Aura Script -----------------------------------------------------------
// Tir Chonaill - Beginner Area (125) (Forest of Souls)
//--- Description -----------------------------------------------------------
// Region you are warped to after talking to Nao/Tin.
//---------------------------------------------------------------------------

public class TirBeginnerRegionScript : RegionScript
{
	public override void LoadWarps()
	{
		// Tir
		SetPropBehavior(0x00A0007D00060018, PropWarp(125,27753,72762, 1, 15250, 38467));
		
		// Gargoyles
		SetPropBehavior(0x00A0007D0001003A, PropWarp(125,19971,69993, 125,17186,69763));
		SetPropBehavior(0x00A0007D0001003B, PropWarp(125,17641,69874, 125,20453,70023));
	}
	
	public override void LoadSpawns()
	{
		// ...
	}

	public override void LoadEvents()
	{
		// "Altar" near Tin
		OnClientEvent(0x00B0007D0001009C, SignalType.Enter, (creature, eventData) =>
		{
			// Only do this once.
			if (creature.Vars.Perm["TinCutscene"] != null) return;

			if (!creature.Quests.Has(202001))
				creature.Quests.Start(202001); // Nao's Letter of Introduction

			var cutscene = new Cutscene("tuto_meet_tin", creature);
			cutscene.AddActor("me", creature);
			cutscene.AddActor("#tin", creature.Region.GetCreature("_tin"));
			cutscene.Play((scene) =>
			{
				// Give first weapon
				if(creature.RightHand == null)
					creature.Inventory.Add(40005, Pocket.RightHand1); // Short Sword

				// Set as soon as the player got everything
				creature.Vars.Perm["TinCutscene"] = true;

				// Required to remove the fade effect.
				scene.Leader.Warp(125, 22930, 75423);
			});
		});
	}
}
