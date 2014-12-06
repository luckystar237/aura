// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Aura.Shared.Mabi;

namespace Aura.Channel.World.Entities
{
	/// <summary>
	/// An entity is any being or object that can be sent in Entity(Dis)Appears.
	/// </summary>
	public abstract class Entity
	{
		public long EntityId { get; set; }
		public string EntityIdHex { get { return this.EntityId.ToString("X16"); } }

		public abstract int RegionId { get; set; }
		public Region Region { get; set; }

		public abstract DataType DataType { get; }

		public abstract Position GetPosition();

		public bool Is(DataType type) { return (this.DataType == type); }

		/// <summary>
		/// Helper method to register this instance for removal after the given time.
		/// </summary>
		/// <param name="disappearTime">The disappear time.</param>
		public void RegisterRemoval(DateTime disappearTime)
		{
			Action<ErinnTime> removal = null;
			removal = (t) =>
			{
				if (t.DateTime > disappearTime)
				{
					if (this.Region != null && this.Region.Contains(this.EntityId))
						this.RemoveFromRegion(this.Region);

					ChannelServer.Instance.World.Heartbeat -= removal;
				}
			};

			ChannelServer.Instance.World.Heartbeat += removal;
		}

		/// <summary>
		/// Any code needed to remove this instance from the region.
		/// </summary>
		/// <param name="region">The region.</param>
		protected abstract void RemoveFromRegion(Region region);
	}

	/// <summary>
	/// Vague entity data type, used in EntityAppears.
	/// </summary>
	public enum DataType : short { Creature = 16, Item = 80, Prop = 160 }
}
