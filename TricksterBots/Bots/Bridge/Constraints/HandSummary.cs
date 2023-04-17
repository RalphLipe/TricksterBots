﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	public class SuitSummary
	{
		internal (int Min, int Max) _quality;

		public (int Min, int Max) Shape { get;  set; }
		public (int Min, int Max) DummyPoints { get; set; }

		public (int Min, int Max) LongHandPoints { get; set; }

		public (SuitQuality Min, SuitQuality Max) Quality
		{ 
			get { return ((SuitQuality)_quality.Min, (SuitQuality)_quality.Max); }
			set { _quality.Min = (int)value.Min; _quality.Max = (int) value.Max; }
		}



		public SuitSummary()
		{
			this.Shape = (0, 13);
			this.DummyPoints = (0, 40);
			this.LongHandPoints = (0, 40);
			this.Quality = (SuitQuality.Poor, SuitQuality.Solid);
		}
		// TODO: There are other properties like "Stopped", "Has Ace", that can go here...
	
		public SuitSummary(SuitSummary other)
		{
			this.Shape = other.Shape;
			this.DummyPoints = other.DummyPoints;
			this.LongHandPoints = other.LongHandPoints;
			this.Quality = other.Quality;
		}

		private static (int Min, int Max) IntersectRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Max(r1.Min, r2.Min), Math.Min(r1.Max, r2.Max));
		}

		// TODO: This is duplicated code.  Figure out a decnent place for it
		private static (int Min, int Max) UnionRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Min(r1.Min, r2.Min), Math.Max(r1.Max, r2.Max));
		}

		internal void Union(SuitSummary other)
		{
			this.Shape = UnionRange(this.Shape, other.Shape);
			this.DummyPoints = UnionRange(this.DummyPoints, other.DummyPoints);
			this.LongHandPoints = UnionRange(this.LongHandPoints, other.LongHandPoints);
			this._quality = UnionRange(this._quality, other._quality);
		}


		internal void Intersect(SuitSummary other)
		{
			this.Shape = IntersectRange(this.Shape, other.Shape);
			this.DummyPoints = IntersectRange(this.DummyPoints, other.DummyPoints);
			this.LongHandPoints = IntersectRange(this.LongHandPoints, other.LongHandPoints);
			this._quality = IntersectRange(this._quality, other._quality);
		}
	}

	public class HandSummary
	{

		private static Suit[] strains = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades, Suit.Unknown };

		public (int Min, int Max) OpeningPoints { get; set; }

		public bool? IsBalanced { get; set; }

		public bool? IsFlat { get; set; }

		// TODO: Perhaps things like this:
		public int? CountAces { get; set; }
			
		public int? CountKings { get; set; }

		public Dictionary<Suit, SuitSummary> Suits { get; protected set; }

		public HandSummary()
		{
			this.OpeningPoints = (0, int.MaxValue);
			this.IsBalanced = null;
			this.IsFlat = null;
			this.CountAces = null;
			this.CountKings = null;
			this.Suits = new Dictionary<Suit, SuitSummary>();
			foreach (Suit suit in strains)
			{
				Suits[suit] = new SuitSummary();
			}
		}

		public HandSummary(HandSummary other)
		{
			this.OpeningPoints = other.OpeningPoints;
			this.IsBalanced = other.IsBalanced;
			this.IsFlat = other.IsFlat;
			this.CountAces = other.CountAces;
			this.CountKings = other.CountKings;
			this.Suits = new Dictionary<Suit, SuitSummary>();
			foreach (Suit suit in strains)
			{
				Suits[suit] = new SuitSummary(other.Suits[suit]);
			}
		}


		private static (int Min, int Max) UnionRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Min(r1.Min, r2.Min), Math.Max(r1.Max, r2.Max));
		}

		private static bool? UnionBool(bool? b1, bool? b2)
		{
			return (b1 == null || b2 == null || b1 != b2) ? null : b1;
		}

		private static int? UnionInt(int? i1, int? i2)
		{
			return (i1 == null || i2 == null || i1 != i2) ? null : i1;
		}


		public void Union(HandSummary other)
		{
			this.OpeningPoints = UnionRange(this.OpeningPoints, other.OpeningPoints);
			this.IsBalanced = UnionBool(this.IsBalanced, other.IsBalanced);
			this.IsFlat = UnionBool(this.IsFlat, other.IsFlat);
			this.CountAces = UnionInt(this.CountAces, other.CountAces);
			this.CountKings = UnionInt(this.CountKings, other.CountKings);
			foreach (var suit in strains)
			{
				this.Suits[suit].Union(other.Suits[suit]);
			}
			TrimShape();
		}

		private void TrimShape()
		{
			int claimed = 0;
			foreach (var suit in BasicBidding.BasicSuits)
			{
				claimed += Suits[suit].Shape.Min;
			}
			foreach (var suit in BasicBidding.BasicSuits)
			{
				var shape = Suits[suit].Shape;
				Suits[suit].Shape = (shape.Min, shape.Max - claimed + shape.Min);
			}
		}

		private static (int Min, int Max) IntersectRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Max(r1.Min, r2.Min), Math.Min(r1.Max, r2.Max));
		}
		private static bool? IntersectBool(bool? b1, bool? b2)
		{ 
			return (b1 == null || b2 == null || b1 != b2) ? null : b1;
		}

		private static int? IntersectInt(int? v1, int? v2)
		{
			return (v1 == null || v2 == null || v1 != v2) ? null : v1;
		}

		public void Intersect(HandSummary other)
		{
			this.OpeningPoints = IntersectRange(this.OpeningPoints, other.OpeningPoints);
			this.IsBalanced = IntersectBool(this.IsBalanced, other.IsBalanced);
			this.IsFlat = IntersectBool(this.IsFlat, other.IsFlat);
			this.CountAces = IntersectInt(this.CountAces, other.CountAces);
			this.CountKings = IntersectInt(this.CountKings, other.CountKings);
			foreach (var suit in strains)
			{
				this.Suits[suit].Intersect(other.Suits[suit]);
			}
		}
	}

}