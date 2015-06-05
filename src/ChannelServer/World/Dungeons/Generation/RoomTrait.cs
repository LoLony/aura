﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;

namespace Aura.Channel.World.Dungeons.Generation
{
	public class RoomTrait
	{
		public RoomTrait[] Neighbor { get; private set; }

		/// <summary>
		/// Paths
		/// </summary>
		public int[] Links { get; private set; }

		/// <summary>
		/// Types of the room's doors (up/down).
		/// </summary>
		public int[] DoorType { get; private set; }

		public RoomType RoomType { get; set; }

		//public int ShapeType { get; private set; }
		//public int ShapeRotationCount { get; private set; }

		public RoomTrait()
		{
			this.Neighbor = new RoomTrait[4];
			this.Links = new int[] { 0, 0, 0, 0 };
			this.DoorType = new int[] { 0, 0, 0, 0 };
		}

		public void SetNeighbor(int direction, RoomTrait room)
		{
			this.Neighbor[direction] = room;
		}

		public bool IsLinked(int direction)
		{
			if (direction > 3)
				throw new ArgumentException("Direction out of bounds.");

			return this.Links[direction] != 0;
		}

		public int GetDoorType(int direction)
		{
			if (direction > 3)
				throw new ArgumentException("Direction out of bounds.");

			return this.DoorType[direction];
		}

		public void Link(int direction, int linkType)
		{
			if (direction > 3)
				throw new ArgumentException("Direction out of bounds.");

			this.Links[direction] = linkType;

			if (this.Neighbor[direction] != null)
			{
				int opposite_direction = Direction.GetOppositeDirection(direction);
				if (linkType == 1)
					this.Neighbor[direction].Links[opposite_direction] = 2;
				else if (linkType == 2)
					this.Neighbor[direction].Links[opposite_direction] = 1;
				else
					this.Neighbor[direction].Links[opposite_direction] = 0;
			}
		}

		public void SetDoorType(int direction, int doorType)
		{
			if (direction > 3)
				throw new ArgumentException("Direction out of bounds.");

			this.DoorType[direction] = doorType;

			var opposite_direction = Direction.GetOppositeDirection(direction);

			var room = this.Neighbor[direction];
			if (room != null)
				room.DoorType[opposite_direction] = doorType;
		}
	}

	public enum RoomType
	{
		None,
		Alley,
		Start,
		End,
		Room,
	}
}