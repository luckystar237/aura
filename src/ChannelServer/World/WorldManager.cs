﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aura.Channel.World.Entities;
using Aura.Data;
using Aura.Shared.Mabi;
using Aura.Shared.Util;
using Aura.Channel.Util;
using Aura.Channel.Network.Sending;
using Aura.Shared.Network;
using System.Threading.Tasks;

namespace Aura.Channel.World
{
	public class WorldManager : IDisposable
	{
		private Dictionary<int, Region> _regions;

		/// <summary>
		/// Returns number of regions.
		/// </summary>
		public int Count { get { return _regions.Count; } }

		public event Action<ErinnTime> Heartbeat;

		public WorldManager()
		{
			_regions = new Dictionary<int, Region>();
		}

		~WorldManager()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_heartbeatTimer.Dispose();
			}
		}

		/// <summary>
		/// Initializes world (regions, heartbeat, etc)
		/// </summary>
		public void Initialize()
		{
			if (_initialized)
				throw new Exception("WorldManager should only be initialized once.");

			this.AddRegionsFromData();
			this.SetUpHeartbeat();

			_initialized = true;
		}

		/// <summary>
		/// Adds all regions found in RegionDb.
		/// </summary>
		private void AddRegionsFromData()
		{
			Parallel.ForEach(AuraData.RegionDb.Entries.Values, region => this.AddRegion(region.Id));
		}

		// ------------------------------------------------------------------

		/// <summary>
		/// Due time of the heartbeat timer.
		/// </summary>
		public const int HeartbeatPeriod = 500;
		public const int Second = 1000, Minute = Second * 60, Hour = Minute * 60;
		public const int ErinnMinute = 1500, ErinnHour = ErinnMinute * 60, ErinnDay = ErinnHour * 24;

		private bool _initialized;
		private Timer _heartbeatTimer;
		private DateTime _lastHeartbeat, _secondsTime, _minutesTime, _hoursTime;
		private ErinnTime _erinnTime;
		private int _mabiTickCount;

		/// <summary>
		/// Initializes heartbeat timer.
		/// </summary>
		private void SetUpHeartbeat()
		{
			var now = DateTime.Now;
			_lastHeartbeat = _secondsTime = _minutesTime = _hoursTime = DateTime.MinValue;
			_erinnTime = new ErinnTime(DateTime.MinValue);

			// Start timer on the next HeartbeatPeriod
			// (eg on the next full 500 ms) and run it regularly afterwards.
			_heartbeatTimer = new Timer(Pulse, null, HeartbeatPeriod - (now.Ticks / 10000 % HeartbeatPeriod), HeartbeatPeriod);
		}

		/// <summary>
		/// Handles regularly occuring events and raises time events.
		/// </summary>
		/// <remarks>
		/// On the first call all time events are raised,
		/// because lastHeartbeat is 0, and the events depend on the time
		/// since the last heartbeat. This also ensures that they aren't
		/// called multiple times.
		/// </remarks>
		private void Pulse(object _)
		{
			var now = new ErinnTime(DateTime.Now);
			var diff = (now.DateTime - _lastHeartbeat);

			if (diff.TotalMilliseconds > HeartbeatPeriod*2 && diff.TotalMilliseconds < 100000000)
			{
				Log.Warning("OMG, the server has an irregular heartbeat! ({0})", diff.ToString());
			}

			// Seconds event
			if ((now.DateTime - _secondsTime).TotalSeconds >= 1)
			{
				ChannelServer.Instance.Events.OnSecondsTimeTick(now);
				_secondsTime = now.DateTime;
			}

			// Minutes event
			if ((now.DateTime - _minutesTime).TotalMinutes >= 1)
			{
				_minutesTime = now.DateTime;
				ChannelServer.Instance.Events.OnMinutesTimeTick(now);

				// Mabi tick event
				// TODO: Each entity should probably track this on its own
				// otherwise all egos will get hungry at the same time, etc
				if (++_mabiTickCount >= 5)
				{
					ChannelServer.Instance.Events.OnMabiTick(now);
					_mabiTickCount = 0;
				}
			}

			// Hours event
			if ((now.DateTime - _hoursTime).TotalHours >= 1)
			{
				ChannelServer.Instance.Events.OnHoursTimeTick(now);
				_hoursTime = now.DateTime;
			}

			// Erinn time event
			if ((now.DateTime - _erinnTime.DateTime).TotalMilliseconds >= ErinnMinute)
			{
				ChannelServer.Instance.Events.OnErinnTimeTick(now);

				// Erinn daytime event
				if (now.IsNight != _erinnTime.IsNight)
				{
					ChannelServer.Instance.Events.OnErinnDaytimeTick(now);
					OnErinnDaytimeTick(now);
				}

				// Erinn midnight event
				if (now.Day > _erinnTime.Day)
					ChannelServer.Instance.Events.OnErinnMidnightTick(now);

				_erinnTime = now;
			}

			if (this.Heartbeat != null)
				this.Heartbeat(now);

			_lastHeartbeat = now.DateTime;
		}

		/// <summary>
		/// Broadcasts Eweca notice, called at 6:00 and 18:00.
		/// </summary>
		/// <param name="now"></param>
		private static void OnErinnDaytimeTick(ErinnTime now)
		{
			var notice = now.IsNight
				? Localization.Get("Eweca is rising.\nMana is starting to fill the air all around.")
				: Localization.Get("Eweca has disappeared.\nThe surrounding Mana is starting to fade away.");
			Send.Notice(NoticeType.MiddleTop, notice);
		}

		// ------------------------------------------------------------------

		/// <summary>
		/// Adds new region with regionId.
		/// </summary>
		/// <param name="regionId"></param>
		public void AddRegion(int regionId)
		{
			lock (_regions)
			{
				if (_regions.ContainsKey(regionId))
				{
					Log.Warning("Region '{0}' already exists.", regionId);
					return;
				}
			}

			var region = new Region(regionId);
			lock (_regions)
				_regions.Add(regionId, region);
		}

		/// <summary>
		/// Removes region with RegionId.
		/// </summary>
		/// <param name="regionId"></param>
		public void RemoveRegion(int regionId)
		{
			lock (_regions)
				_regions.Remove(regionId);
		}

		/// <summary>
		/// Returns region by id, or null if it doesn't exist.
		/// </summary>
		/// <param name="regionId"></param>
		/// <returns></returns>
		public Region GetRegion(int regionId)
		{
			Region result;
			lock (_regions)
				_regions.TryGetValue(regionId, out result);
			return result;
		}

		/// <summary>
		/// Returns true if region exists.
		/// </summary>
		/// <param name="regionId"></param>
		/// <returns></returns>
		public bool HasRegion(int regionId)
		{
			return _regions.ContainsKey(regionId);
		}

		/// <summary>
		/// Returns first prop with the given id, from any region,
		/// or null, if none was found.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Prop GetProp(long id)
		{
			return _regions.Values.Select(region => region.GetProp(id)).FirstOrDefault(prop => prop != null);
		}

		/// <summary>
		/// Returns player creature with the given name, or null.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public PlayerCreature GetPlayer(string name)
		{
			return _regions.Values.Select(region => region.GetPlayer(name)).FirstOrDefault(creature => creature != null);
		}

		/// <summary>
		/// Returns all players in all regions.
		/// </summary>
		/// <returns></returns>
		public List<Creature> GetAllPlayers()
		{
			var result = new List<Creature>();

			foreach (var region in _regions.Values)
				result.AddRange(region.GetAllPlayers());

			return result;
		}

		/// <summary>
		/// Returns amount of players in all regions.
		/// </summary>
		/// <returns></returns>
		public int CountPlayers()
		{
			return _regions.Values.Sum(region => region.CountPlayers());
		}

		/// <summary>
		/// Returns creature from any region by id, or null.
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public Creature GetCreature(long entityId)
		{
			return _regions.Values.Select(region => region.GetCreature(entityId)).FirstOrDefault(creature => creature != null);
		}

		/// <summary>
		/// Returns creature from any region by name, or null.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Creature GetCreature(string name)
		{
			return _regions.Values.Select(region => region.GetCreature(name)).FirstOrDefault(creature => creature != null);
		}

		/// <summary>
		/// Returns NPC from any region by id, or null.
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public NPC GetNpc(long entityId)
		{
			return _regions.Values.Select(region => region.GetNpc(entityId)).FirstOrDefault(creature => creature != null);
		}

		/// <summary>
		/// Returns collection of all good, normal NPCs.
		/// </summary>
		/// <returns></returns>
		public ICollection<Creature> GetAllGoodNpcs()
		{
			var result = new List<Creature>();

			foreach (var region in _regions.Values)
				region.GetAllGoodNpcs(ref result);

			return result;
		}

		/// <summary>
		/// Removes all NPCs, props, etc from all regions.
		/// </summary>
		public void RemoveScriptedEntities()
		{
			foreach (var region in _regions.Values)
				region.RemoveScriptedEntities();
		}

		/// <summary>
		/// Broadcasts packet in all regions.
		/// </summary>
		/// <param name="packet"></param>
		public void Broadcast(Packet packet)
		{
			foreach (var region in _regions.Values)
				region.Broadcast(packet);
		}
	}
}
